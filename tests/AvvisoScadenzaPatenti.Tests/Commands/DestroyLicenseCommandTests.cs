namespace AvvisoScadenzaPatenti.Tests;

using AvvisoScadenzaPatenti.Cli;
using AvvisoScadenzaPatenti.Cli.Commands;
using AvvisoScadenzaPatenti.Core.Entities;
using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Shared;
using Moq;
using Xunit;

public class DestroyLicenseCommandTests
{
    private readonly Mock<ILicenseRepository> _licenseRepoMock = new();

    private DestroyLicenseCommand CreateSut() => new(_licenseRepoMock.Object);

    private static License CreateLicense(string number, string status) => new()
    {
        Office = "Comando",
        LicenseType = "T",
        Category = "PRIMA CATEGORIA",
        LicenseNumber = number,
        CardNumber = "1",
        LastName = "ROSSI",
        FirstName = "MARIO",
        ExpiryDate = DateTime.Today.AddDays(10),
        Status = status
    };

    [Fact]
    public async Task ExecuteAsync_WithUnknownLicense_ReturnsError_AndDoesNotSave()
    {
        _licenseRepoMock.Setup(r => r.GetByLicenseNumber("99999")).Returns((License?)null);
        var opts = new Options { DestroyLicenseNumber = "99999" };

        var result = await CreateSut().ExecuteAsync(opts, CancellationToken.None);

        Assert.Equal(1, result);
        _licenseRepoMock.Verify(r => r.SaveAll(It.IsAny<IEnumerable<License>>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveLicense_SetsDestroyedStatus_AndSaves()
    {
        var license = CreateLicense("27815", "PATENTE EMESSA");
        var all = new List<License> { license };
        _licenseRepoMock.Setup(r => r.GetByLicenseNumber("27815")).Returns(license);
        _licenseRepoMock.Setup(r => r.GetAll()).Returns(all);
        var opts = new Options { DestroyLicenseNumber = " 27815 " };

        var result = await CreateSut().ExecuteAsync(opts, CancellationToken.None);

        Assert.Equal(0, result);
        Assert.Equal(LicenseStatus.Destroyed, license.Status);
        Assert.True(license.IsDestroyed());
        _licenseRepoMock.Verify(r => r.SaveAll(all), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithAlreadyDestroyedLicense_ReturnsSuccess_AndDoesNotSave()
    {
        var license = CreateLicense("27815", "distrutta");
        _licenseRepoMock.Setup(r => r.GetByLicenseNumber("27815")).Returns(license);
        var opts = new Options { DestroyLicenseNumber = "27815" };

        var result = await CreateSut().ExecuteAsync(opts, CancellationToken.None);

        Assert.Equal(0, result);
        _licenseRepoMock.Verify(r => r.SaveAll(It.IsAny<IEnumerable<License>>()), Times.Never);
    }
}
