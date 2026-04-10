namespace AvvisoScadenzaPatenti.Tests;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Models;

using FluentAssertions;

using Moq;

public class EmployeeRepositoryTests
{
    [Fact]
    public async Task GetAll_WhenNoEmployeesExist_ShouldReturnEmptyList()
    {
        // Arrange
        var mockRepo = new Mock<IEmployeeRepository>();
        mockRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Employee>()); // Simula DB vuoto

        // Act
        var result = await mockRepo.Object.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
        mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }
}