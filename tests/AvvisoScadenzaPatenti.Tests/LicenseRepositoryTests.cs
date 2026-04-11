namespace AvvisoScadenzaPatenti.Tests;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Models;

using FluentAssertions;

using Moq;

public class LicenseRepositoryTests
{
    [Fact]
    public void GetAll_WhenNoLicensesExist_ShouldReturnEmptyList()
    {
        // Arrange
        var mockRepo = new Mock<ILicenseRepository>();
        mockRepo.Setup(r => r.GetAll())
                .Returns(new List<License>()); // Simula DB vuoto

        // Act
        var result = mockRepo.Object.GetAll();

        // Assert
        result.Should().BeEmpty();
        mockRepo.Verify(r => r.GetAll(), Times.Once);
    }

    [Fact]
    public void GetByLicenseNumber_ShouldReturnCorrectLicense()
    {
        // Arrange
        var mockRepo = new Mock<ILicenseRepository>();
        var expectedLicense = new License
        {
            Category = "PRIMA CATEGORIA",
            LicenseNumber = "27815",
            ExpiryDate = DateTime.Now
        };

        mockRepo.Setup(r => r.GetByLicenseNumber("27815"))
                .Returns(expectedLicense);

        // Act
        var result = mockRepo.Object.GetByLicenseNumber("27815");

        // Asserts result is not null AND continues the assertion on the object itself
        result.Should().NotBeNull()
            .And.Subject.As<License>()
            .Category.Should().Be("PRIMA CATEGORIA");  
    }
}