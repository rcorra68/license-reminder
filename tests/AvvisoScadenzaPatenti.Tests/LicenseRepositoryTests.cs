namespace AvvisoScadenzaPatenti.Tests;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Models;

using FluentAssertions;

using Moq;

public class LicenseRepositoryTests
{
    [Fact]
    public async Task GetAll_WhenNoLicensesExist_ShouldReturnEmptyList()
    {
        // Arrange
        var mockRepo = new Mock<ILicenseRepository>();
        mockRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<License>()); // Simula DB vuoto

        // Act
        var result = await mockRepo.Object.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
        mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByLicenseNumber_ShouldReturnCorrectLicense()
    {
        // Arrange
        var mockRepo = new Mock<ILicenseRepository>();
        var expectedLicense = new License
        {
            Category = "PRIMA CATEGORIA",
            LicenseNumber = "27815",
            ExpiryDate = DateTime.Now
        };

        mockRepo.Setup(r => r.GetByLicenseNumberAsync("27815"))
                .ReturnsAsync(expectedLicense);

        // Act
        var result = await mockRepo.Object.GetByLicenseNumberAsync("27815");

        // Assert
        result.Should().NotBeNull();
        result.Category.Should().Be("PRIMA CATEGORIA");
    }
}