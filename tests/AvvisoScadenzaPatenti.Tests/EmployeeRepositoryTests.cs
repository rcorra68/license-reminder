namespace AvvisoScadenzaPatenti.Tests;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Entities;

using FluentAssertions;

using Moq;

public class EmployeeRepositoryTests
{
    [Fact]
    public void GetAll_WhenNoEmployeesExist_ShouldReturnEmptyList()
    {
        // Arrange
        var mockRepo = new Mock<IEmployeeRepository>();
        mockRepo.Setup(r => r.GetAll())
                .Returns(new List<Employee>()); // Simula DB vuoto

        // Act
        var result = mockRepo.Object.GetAll();

        // Assert
        result.Should().BeEmpty();
        mockRepo.Verify(r => r.GetAll(), Times.Once);
    }
}