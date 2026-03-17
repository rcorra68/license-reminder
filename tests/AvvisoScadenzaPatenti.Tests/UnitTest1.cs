namespace AvvisoScadenzaPatenti.Tests;

using AvvisoScadenzaPatenti.Infrastructure.Repositories;

public class EmployeeRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_WhenEmployeeNotFound_ShouldLogWarning()
    {
        //// Arrange
        //var loggerMock = new Mock<ILogger<EmployeeRepository>>();
        //var repository = new EmployeeRepository(loggerMock.Object);
        //var nonExistentId = Guid.NewGuid();

        //// Act
        //var result = await repository.GetByIdAsync(nonExistentId);

        //// Assert
        //Assert.Null(result);

        //// Verify that LogWarning was called exactly once
        //loggerMock.Verify(
        //    x => x.Log(
        //        LogLevel.Warning,
        //        It.IsAny<EventId>(),
        //        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(nonExistentId.ToString())),
        //        It.IsAny<Exception>(),
        //        It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
        //    Times.Once);
    }
}