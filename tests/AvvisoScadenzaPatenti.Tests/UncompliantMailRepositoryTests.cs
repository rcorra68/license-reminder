namespace AvvisoScadenzaPatenti.Tests;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Models;

using FluentAssertions;

using Moq;

public class UncompliantMailRepositoryTests
{
    [Fact]
    public async Task GetAll_WhenNoUncompliantMailsExist_ShouldReturnEmptyList()
    {
        // Arrange
        var mockRepo = new Mock<IUncompliantMailRepository>();
        mockRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<UncompliantMail>()); // Simula DB vuoto

        // Act
        var result = await mockRepo.Object.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
        mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }
}