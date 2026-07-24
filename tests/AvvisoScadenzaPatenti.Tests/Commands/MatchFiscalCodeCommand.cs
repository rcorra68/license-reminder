namespace AvvisoScadenzaPatenti.Cli.Tests.Commands;

using AvvisoScadenzaPatenti.Cli.Commands;
using AvvisoScadenzaPatenti.Core.Entities;
using AvvisoScadenzaPatenti.Core.Interfaces;
using Moq;
using Xunit;

public class MatchFiscalCodeCommandTests
{
    private readonly Mock<IEmployeeRepository> _employeeRepoMock = new();

    private MatchFiscalCodeCommand CreateSut() => new(_employeeRepoMock.Object);

    [Fact]
    public async Task ExecuteAsync_WithTooShortFiscalCode_ReturnsError_AndDoesNotTouchRepository()
    {
        // Arrange
        var opts = new Options { MatchCf = "ABC123" }; // < 11 caratteri
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(opts, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
        _employeeRepoMock.Verify(r => r.GetAll(), Times.Never);
        _employeeRepoMock.Verify(r => r.Update(It.IsAny<Employee>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoCandidates_ReturnsError_AndDoesNotUpdate()
    {
        // Arrange
        _employeeRepoMock.Setup(r => r.GetAll()).Returns(new List<Employee>());
        var opts = new Options { MatchCf = "CRRRRT68E23A271X" }; // sostituisci con un CF valido di test
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(opts, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
        _employeeRepoMock.Verify(r => r.Update(It.IsAny<Employee>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleCandidate_UpdatesEmployee_AndReturnsSuccess()
    {
        // Arrange: un solo dipendente che combacia con cognome+nome codificati nel CF
        var employee = new Employee { FirstName = "Mario", LastName = "Rossi", Mail ="mario.rossi@vigilfuoco.it" /* ... */ };
        _employeeRepoMock.Setup(r => r.GetAll()).Returns(new List<Employee> { employee });

        var opts = new Options { MatchCf = "RSSMRA80A01H501U" };
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(opts, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
        _employeeRepoMock.Verify(r => r.Update(It.Is<Employee>(e => e == employee)), Times.Once);
        Assert.Equal("RSSMRA80A01H501U", employee.FiscalCode);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleCandidates_AndNoResolveIndex_ListsCandidates_AndDoesNotUpdate()
    {
        // Arrange: due omonimi
        var candidate1 = new Employee { FirstName = "Mario", LastName = "Rossi", Mail = "mario.rossi@vigilfuoco.it" };
        var candidate2 = new Employee { FirstName = "Mario", LastName = "Rossi", Mail = "mario1.rossi@vigilfuoco.it" };
        _employeeRepoMock.Setup(r => r.GetAll()).Returns(new List<Employee> { candidate1, candidate2 });

        var opts = new Options { MatchCf = "RSSMRA80A01H501U", ResolveIndex = null };
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(opts, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
        _employeeRepoMock.Verify(r => r.Update(It.IsAny<Employee>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleCandidates_AndValidResolveIndex_UpdatesOnlySelectedCandidate()
    {
        // Arrange
        var candidate1 = new Employee { FirstName = "Mario", LastName = "Rossi", Mail = "mario.rossi@vigilfuoco.it" };
        var candidate2 = new Employee { FirstName = "Mario", LastName = "Rossi", Mail = "mario1.rossi@vigilfuoco.it" };
        _employeeRepoMock.Setup(r => r.GetAll()).Returns(new List<Employee> { candidate1, candidate2 });

        var opts = new Options { MatchCf = "RSSMRA80A01H501U", ResolveIndex = 2 };
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(opts, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
        _employeeRepoMock.Verify(r => r.Update(It.Is<Employee>(e => e == candidate2)), Times.Once);
        _employeeRepoMock.Verify(r => r.Update(It.Is<Employee>(e => e == candidate1)), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithResolveIndexOutOfRange_ReturnsError_AndDoesNotUpdate()
    {
        // Arrange
        var candidate1 = new Employee { FirstName = "Mario", LastName = "Rossi", Mail = "mario.rossi@vigilfuoco.it" };
        var candidate2 = new Employee { FirstName = "Mario", LastName = "Rossi", Mail = "mario.rossi1@vigilfuoco.it" };
        _employeeRepoMock.Setup(r => r.GetAll()).Returns(new List<Employee> { candidate1, candidate2 });

        var opts = new Options { MatchCf = "RSSMRA80A01H501U", ResolveIndex = 5 };
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(opts, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
        _employeeRepoMock.Verify(r => r.Update(It.IsAny<Employee>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CallsUpdateExactlyOnce_NeverCallsAnyOtherRepositoryMethod()
    {
        // Questo test è pensato apposta per il bug che avete trovato:
        // verifica che il comando chiami SOLO GetAll() + Update(), e nient'altro
        // che possa spiegare la cancellazione del record. Se in futuro l'interfaccia
        // IEmployeeRepository espone un metodo Delete/SaveAll, aggiungi qui una
        // Verify(..., Times.Never) su quello.
        var employee = new Employee { FirstName = "Mario", LastName = "Rossi", Mail = "mario.rossi@vigilfuoco.it" };
        _employeeRepoMock.Setup(r => r.GetAll()).Returns(new List<Employee> { employee });

        var opts = new Options { MatchCf = "RSSMRA80A01H501U" };
        var sut = CreateSut();

        await sut.ExecuteAsync(opts, CancellationToken.None);

        _employeeRepoMock.Verify(r => r.GetAll(), Times.Once);
        _employeeRepoMock.Verify(r => r.Update(It.IsAny<Employee>()), Times.Once);
        _employeeRepoMock.VerifyNoOtherCalls();
    }
}