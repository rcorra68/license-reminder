using AvvisoScadenzaPatenti.Core.Configuration;
using AvvisoScadenzaPatenti.Core.Entities;
using AvvisoScadenzaPatenti.Core.Models;
using AvvisoScadenzaPatenti.Infrastructure.Services.Mail;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace AvvisoScadenzaPatenti.Tests.Infrastructure;

public class MailKitEmailServiceTests
{
    private readonly Mock<ILogger<MailKitEmailService>> _loggerMock;
    private readonly IOptions<AppSettings> _options;
    private readonly AppSettings _settings;

    public MailKitEmailServiceTests()
    {
        _loggerMock = new Mock<ILogger<MailKitEmailService>>();
        _settings = new AppSettings
        {
            AdminEmail = "admin@test.com",
            Smtp = new SmtpSettings
            {
                Host = "localhost",
                Port = 25
            },
            MailBcc = ["bcc@test.com"]
        };
        _options = Options.Create(_settings);
    }

    [Fact]
    public async Task SendExpirationNoticeAsync_ShouldSkip_WhenEmailIsEmpty()
    {
        // Arrange
        var service = new MailKitEmailService(_options, _loggerMock.Object);
        var employee = new Employee { FirstName = "John", LastName = "Doe", Mail = "" };
        var license = new License { LicenseNumber = "12345", ExpiryDate = DateTime.Now.AddDays(10) };

        // Act
        await service.SendExpirationNoticeAsync(employee, license, false);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Skipped email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenSmtpConfigMissing()
    {
        // Arrange
        var invalidSettings = Options.Create(new AppSettings { Smtp = null! });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new MailKitEmailService(invalidSettings, _loggerMock.Object));
    }

    [Fact]
    public async Task SendDailySummaryReportAsync_ShouldSkip_WhenAdminEmailNotConfigured()
    {
        // Arrange
        _settings.AdminEmail = string.Empty;
        var service = new MailKitEmailService(_options, _loggerMock.Object);
        var report = new DailyReport { ExecutionDate = DateTime.Now };

        // Act
        await service.SendDailySummaryReportAsync(report);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AdminEmail not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}