namespace AvvisoScadenzaPatenti.Infrastructure.Services.Mail;

using System.Text;

using AvvisoScadenzaPatenti.Core.Configuration;
using AvvisoScadenzaPatenti.Core.Entities;
using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Models;

using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MimeKit;

/// <summary>
/// Implements an email service using MailKit to send license expiration notices,
/// daily summary reports, and perform SMTP connectivity health checks.
/// </summary>
/// <remarks>
/// This service relies on <see cref="AppSettings"/> for SMTP configuration and supports
/// SSL/TLS connections, authentication, BCC copying, and structured logging.
/// </remarks>
public class MailKitEmailService : IEmailService
{
    private readonly AppSettings _settings;
    private readonly ILogger<MailKitEmailService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MailKitEmailService"/> class.
    /// </summary>
    /// <param name="options">Application settings containing SMTP configuration.</param>
    /// <param name="logger">Logger for tracking email operations and errors.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when SMTP configuration is missing.</exception>
    public MailKitEmailService(
        IOptions<AppSettings> options,
        ILogger<MailKitEmailService> logger)
    {
        _settings = options?.Value
            ?? throw new ArgumentNullException(nameof(options));

        _logger = logger;

        if (_settings.Smtp is null)
            throw new InvalidOperationException("SMTP configuration missing.");
    }

    /// <summary>
    /// Sends an expiration notice email to an employee regarding their driving license status.
    /// </summary>
    /// <param name="employee">The employee receiving the notification.</param>
    /// <param name="license">The license being checked for expiration.</param>
    /// <param name="isExpired">True if the license is expired; false if it's expiring soon.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    public async Task SendExpirationNoticeAsync(Employee employee, License license, bool isExpired, CancellationToken ct = default)
    {
        Validate(employee, license);

        if (string.IsNullOrWhiteSpace(employee.Mail))
        {
            _logger.LogWarning("Skipped email: empty recipient for employee {EmployeeFirstName} {EmployeeLLastName}", employee.FirstName, employee.LastName);
            return;
        }

        var message = CreateLicenseMessage(employee, license, isExpired);

        await SendAsync(message, ct);

        _logger.LogInformation(
            "Expiration email sent to {Email} for license {LicenseId}",
            employee.Mail,
            license.LicenseNumber);
    }

    /// <summary>
    /// Verifies SMTP connectivity by sending a health check email.
    /// </summary>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>True if the SMTP connection and send operation succeed; otherwise, false.</returns>
    public async Task<bool> VerifyEmailConnectivityAsync(CancellationToken ct = default)
    {
        var message = CreateHealthCheckMessage();

        try
        {
            await SendAsync(message, ct);
            _logger.LogInformation("SMTP health check succeeded for host {Host}", _settings.Smtp.Host);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP health check failed");
            return false;
        }
    }

    /// <summary>
    /// Sends a daily summary report to the configured admin email address.
    /// </summary>
    /// <param name="report">The daily report containing processed employees, emails sent, errors, and execution time.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    public async Task SendDailySummaryReportAsync(DailyReport report, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (string.IsNullOrWhiteSpace(_settings.AdminEmail))
        {
            _logger.LogWarning("Daily report skipped: AdminEmail not configured");
            return;
        }

        var message = CreateDailyReportMessage(report);

        await SendAsync(message, ct);

        _logger.LogInformation("Daily report sent to {AdminEmail}", _settings.AdminEmail);
    }

    /// <summary>
    /// Sends a <see cref="MimeMessage"/> via SMTP using a disposable <see cref="SmtpClient"/>.
    /// </summary>
    /// <param name="message">The email message to send.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <exception cref="Exception">Re-throws any exception occurring during connection or send with structured logging.</exception>
    private async Task SendAsync(MimeMessage message, CancellationToken ct)
    {
        using var client = new SmtpClient();

        try
        {
            ct.ThrowIfCancellationRequested();

            await ConnectAsync(client, ct);

            ct.ThrowIfCancellationRequested();

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Email sending cancelled for {Subject}", message.Subject);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed for {Subject}", message.Subject);
            throw;
        }
    }

    /// <summary>
    /// Establishes an SMTP connection with configurable security mode and optional authentication.
    /// </summary>
    /// <param name="client">The <see cref="SmtpClient"/> to configure and connect.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when SMTP host is missing.</exception>
    private async Task ConnectAsync(SmtpClient client, CancellationToken ct)
    {
        var smtp = _settings.Smtp;

        if (string.IsNullOrWhiteSpace(smtp.Host))
            throw new InvalidOperationException("SMTP host missing.");

        // Map custom SmtpSecurityMode enum to MailKit's SecureSocketOptions
        var options = smtp.SecurityMode switch
        {
            SmtpSecurityMode.SslOnConnect => SecureSocketOptions.SslOnConnect,
            SmtpSecurityMode.StartTls => SecureSocketOptions.StartTls,
            SmtpSecurityMode.None => SecureSocketOptions.None,
            _ => SecureSocketOptions.Auto
        };

        await client.ConnectAsync(smtp.Host, smtp.Port, options, ct);

        // Authenticate only if username is provided
        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            await client.AuthenticateAsync(
                smtp.Username,
                smtp.Password,
                ct);
        }
    }

    /// <summary>
    /// Creates an HTML email message for license expiration/expired notifications.
    /// </summary>
    private MimeMessage CreateLicenseMessage(Employee employee, License license, bool isExpired)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress("Gestione Patenti", "noreply@vigilfuoco.it"));
        message.To.Add(new MailboxAddress($"{employee.FirstName} {employee.LastName}", employee.Mail));

        AddBcc(message);

        message.Subject = isExpired
            ? $"⚠ Patente SCADUTA - {employee.LastName} {employee.FirstName}"
            : $"Promemoria scadenza patente - {employee.LastName} {employee.FirstName}";

        var body = isExpired
            ? "<b>La tua patente è SCADUTA</b>"
            : $"Patente {license.Category} in scadenza il <b>{license.ExpiryDate:dd/MM/yyyy}</b>";

        message.Body = new BodyBuilder
        {
            HtmlBody = $@"
                <h3>Avviso Scadenza</h3>
                <p>Ciao {employee.FirstName} {employee.LastName},</p>
                <p>{body}</p>
                <br/>
                <small>Email automatica - non rispondere</small>"
        }.ToMessageBody();

        return message;
    }

    /// <summary>
    /// Creates an HTML email message containing the daily processing summary report.
    /// </summary>
    private MimeMessage CreateDailyReportMessage(DailyReport report)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress("License System", "noreply@vigilfuoco.it"));
        message.To.Add(MailboxAddress.Parse(_settings.AdminEmail));

        message.Subject = $"Report giornaliero - {report.ExecutionDate:yyyy-MM-dd}";

        message.Body = new BodyBuilder
        {
            HtmlBody = $@"
                <h2>Report giornaliero</h2>
                <ul>
                    <li>Employee: {report.ProcessedEmployees}</li>
                    <li>Email: {report.EmailsSent}</li>
                    <li>Errori: {report.Errors}</li>
                    <li>Tempo: {report.ExecutionTime.TotalSeconds:n2}s</li>
                </ul>"
        }.ToMessageBody();

        return message;
    }

    /// <summary>
    /// Creates a plain-text email message used for SMTP health check validation.
    /// </summary>
    private MimeMessage CreateHealthCheckMessage()
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress("System", "noreply@vigilfuoco.it"));
        message.To.Add(MailboxAddress.Parse(_settings.AdminEmail ?? "admin@test.local"));

        message.Subject = "SMTP Health Check";

        message.Body = new TextPart("plain")
        {
            Text = $"Health check OK - {DateTime.UtcNow:O}"
        };

        return message;
    }

    /// <summary>
    /// Adds configured BCC addresses to the email message if any are defined.
    /// </summary>
    private void AddBcc(MimeMessage message)
    {
        foreach (var bcc in _settings.MailBcc ?? Enumerable.Empty<string>())
        {
            if (MailboxAddress.TryParse(bcc, out var addr))
                message.Bcc.Add(addr);
        }
    }

    /// <summary>
    /// Validates that employee and license objects are not null before processing.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when employee or license is null.</exception>
    private static void Validate(Employee employee, License license)
    {
        ArgumentNullException.ThrowIfNull(employee);
        ArgumentNullException.ThrowIfNull(license);
    }
}