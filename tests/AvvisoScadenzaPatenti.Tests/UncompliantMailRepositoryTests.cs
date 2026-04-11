namespace AvvisoScadenzaPatenti.Tests;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Entities;

using FluentAssertions;

using Moq;

public class UncompliantMailRepositoryTests
{
    [Fact]
    public void GetAll_WhenNoUncompliantMailsExist_ShouldReturnEmptyList()
    {
        // Arrange
        var mockRepo = new Mock<IUncompliantMailRepository>();
        mockRepo.Setup(r => r.GetAll())
                .Returns(new List<UncompliantMail>()); // Simula DB vuoto

        // Act
        var result = mockRepo.Object.GetAll();

        // Assert
        result.Should().BeEmpty();
        mockRepo.Verify(r => r.GetAll(), Times.Once);
    }
}