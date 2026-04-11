namespace AvvisoScadenzaPatenti.Infrastructure.Services.Mail;

using AvvisoScadenzaPatenti.Core.Configuration;
using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Entities;
using AvvisoScadenzaPatenti.Core.Models;

using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Logging;

using MimeKit;

/// <summary>
/// SMTP-based email service implementation using MailKit.
/// 
/// Responsibilities:
/// - Sending license expiration notifications to employees
/// - Sending administrative daily summary reports
/// - Performing SMTP connectivity health checks
/// 
/// This service acts as an infrastructure adapter between the application
/// and external SMTP providers.
/// </summary>
public class MailKitEmailService : IEmailService
{
    private readonly AppSettings _settings;
    private readonly ILogger<MailKitEmailService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MailKitEmailService"/> class.
    /// </summary>
    /// <param name="settings">Application configuration containing SMTP and email settings.</param>
    /// <param name="logger">Logger instance used for operational diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
    /// <exception cref="ArgumentException">Thrown when SMTP configuration is missing or invalid.</exception>
    public MailKitEmailService(AppSettings settings, ILogger<MailKitEmailService> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger;

        if (_settings.Smtp == null)
            throw new ArgumentException("SMTP configuration is missing in AppSettings.");
    }

    /// <summary>
    /// Sends a license expiration notification email to an employee.
    /// 
    /// This method supports both:
    /// - Upcoming expiration warnings
    /// - Already expired license alerts
    /// 
    /// The email content is dynamically generated based on license state.
    /// </summary>
    /// <param name="employee">Target employee recipient.</param>
    /// <param name="license">License associated with the notification.</param>
    /// <param name="isExpired">Indicates whether the license is already expired.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when SMTP configuration is invalid.</exception>
    public void SendExpirationNotice(Employee employee, License license, bool isExpired)
    {
        ArgumentNullException.ThrowIfNull(employee);
        ArgumentNullException.ThrowIfNull(license);

        if (string.IsNullOrWhiteSpace(employee.Mail))
        {
            _logger.LogWarning("Skipped email sending: employee email is empty for {Employee}", employee.Mail);
            return;
        }

        var message = CreateMessage(employee, license, isExpired);

        using var client = new SmtpClient();

        try
        {
            ConnectAndAuthenticate(client);

            client.Send(message);
            client.Disconnect(true);

            _logger.LogInformation(
                "Email sent successfully to {Email} for license expiring on {ExpiryDate}",
                employee.Mail,
                license.ExpiryDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SMTP error while sending email to {Email}",
                employee.Mail);

            throw;
        }
    }

    /// <summary>
    /// Performs an SMTP connectivity check by sending a test email.
    /// 
    /// WARNING:
    /// This method sends a real email and should be executed only in
    /// development or controlled environments.
    /// </summary>
    /// <returns>
    /// True if SMTP connection, authentication, and email delivery succeed;
    /// otherwise false.
    /// </returns>
    public bool VerifyEmailConnectivity()
    {
        _logger.LogInformation("SMTP health check starting against {Host}", _settings.Smtp.Host);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("License Reminder System", "noreply@vigilfuoco.it"));
        message.To.Add(new MailboxAddress("Admin", _settings.AdminEmail ?? string.Empty));
        message.Subject = "License-Reminder: SMTP Health Check";

        message.Body = new TextPart("plain")
        {
            Text = $"Health check executed at {DateTime.UtcNow:O}"
        };

        using var client = new SmtpClient();

        try
        {
            ConnectAndAuthenticate(client);

            client.Send(message);
            client.Disconnect(true);

            _logger.LogInformation("SMTP health check succeeded.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP health check failed: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Sends a daily operational summary report to the system administrator.
    /// 
    /// The report includes:
    /// - number of processed employees
    /// - number of emails sent
    /// - number of errors encountered
    /// - total execution time
    /// 
    /// This method is intended for operational monitoring and auditing purposes.
    /// </summary>
    /// <param name="report">Aggregated execution metrics of the daily batch process.</param>
    /// <exception cref="ArgumentNullException">Thrown when report is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when SMTP configuration is invalid.</exception>
    public void SendDailySummaryReport(DailyReport report)
    {
        if (report == null)
            throw new ArgumentNullException(nameof(report));

        if (string.IsNullOrWhiteSpace(_settings.AdminEmail))
        {
            _logger.LogWarning("Daily report skipped: AdminEmail is not configured.");
            return;
        }

        var message = new MimeMessage();

        message.From.Add(new MailboxAddress("License Reminder System", "noreply@vigilfuoco.it"));
        message.To.Add(MailboxAddress.Parse(_settings.AdminEmail));

        message.Subject = $"Daily License Report - {report.ExecutionDate:yyyy-MM-dd}";

        message.Body = new BodyBuilder
        {
            HtmlBody = $@"
            <h2>Daily Execution Report</h2>

            <p><b>Date:</b> {report.ExecutionDate:yyyy-MM-dd}</p>

            <ul>
                <li><b>Employees processed:</b> {report.ProcessedEmployees}</li>
                <li><b>Emails sent:</b> {report.EmailsSent}</li>
                <li><b>Errors:</b> {report.Errors}</li>
                <li><b>Execution time:</b> {report.ExecutionTime.TotalSeconds:n2} seconds</li>
            </ul>

            <br/>
            <small>This is an automated system report.</small>"
        }.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            ConnectAndAuthenticate(client);

            client.Send(message);
            client.Disconnect(true);

            _logger.LogInformation("Daily summary report sent successfully to {AdminEmail}", _settings.AdminEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send daily summary report to {AdminEmail}", _settings.AdminEmail);
            throw;
        }
    }

    #region Private helpers

    /// <summary>
    /// Establishes an SMTP connection using the configured security mode
    /// and performs authentication if credentials are provided.
    /// </summary>
    /// <param name="client">SMTP client instance to configure and connect.</param>
    /// <exception cref="InvalidOperationException">Thrown when SMTP host is not configured.</exception>
    private void ConnectAndAuthenticate(SmtpClient client)
    {
        var smtp = _settings.Smtp;

        if (string.IsNullOrWhiteSpace(smtp.Host))
            throw new InvalidOperationException("SMTP host is not configured.");

        var secureSocket = smtp.SecurityMode switch
        {
            SmtpSecurityMode.SslOnConnect => SecureSocketOptions.SslOnConnect,
            SmtpSecurityMode.StartTls => SecureSocketOptions.StartTls,
            SmtpSecurityMode.None => SecureSocketOptions.None,
            _ => SecureSocketOptions.Auto
        };

        client.Connect(smtp.Host, smtp.Port, secureSocket);

        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            client.Authenticate(smtp.Username, smtp.Password);
        }
    }
    
    /// <summary>
    /// Creates a fully formatted MIME email message for license notifications.
    /// 
    /// The message includes:
    /// - recipient personalization
    /// - BCC support
    /// - dynamic subject based on expiration state
    /// - HTML formatted body content
    /// </summary>
    /// <param name="employee">Email recipient.</param>
    /// <param name="license">License data used to generate message content.</param>
    /// <param name="isExpired">Indicates whether the license is expired.</param>
    /// <returns>A fully constructed <see cref="MimeMessage"/> ready to be sent.</returns>
    private MimeMessage CreateMessage(Employee employee, License license, bool isExpired)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress("License Management System", "noreply@vigilfuoco.it"));
        message.To.Add(new MailboxAddress($"{employee.FirstName} {employee.LastName}", employee.Mail));

        foreach (var bcc in _settings.MailBcc ?? Enumerable.Empty<string>())
        {
            if (MailboxAddress.TryParse(bcc, out var addr))
                message.Bcc.Add(addr);
        }

        message.Subject = isExpired
            ? $"WARNING: License EXPIRED - {employee.LastName} {employee.FirstName}"
            : $"Reminder: License expiring - {employee.LastName} {employee.FirstName}";

        var body = isExpired
            ? $"<p><b>LICENSE EXPIRED</b></p>"
            : $"<p>License <b>{license.Category}</b> expires on <b>{license.ExpiryDate:yyyy-MM-dd}</b>.</p>";

        message.Body = new BodyBuilder
        {
            HtmlBody = $@"
                <h3>License Expiration Notice</h3>
                <p>Hello {employee.FirstName} {employee.LastName},</p>
                {body}
                <br/>
                <small>This is an automated message. Please do not reply.</small>"
        }.ToMessageBody();

        return message;
    }

    #endregion
}