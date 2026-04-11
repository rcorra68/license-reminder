namespace AvvisoScadenzaPatenti.Infrastructure.Services.Mail;

using System.Net;
using System.Runtime;
using System.Text;

using AvvisoScadenzaPatenti.Core.Configuration;
using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Entities;

using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using MimeKit;

public class MailKitEmailService : IEmailService
{
    private readonly AppSettings _settings;
    private readonly ILogger<MailKitEmailService> _logger;

    public MailKitEmailService(AppSettings settings, ILogger<MailKitEmailService> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        if (settings.MailServer == null)
            throw new ArgumentException("MailServer configuration is missing", nameof(settings));

        _logger = logger;
    }

    /// <summary>
    /// Sends an expiration notice email using SMTP settings from configuration.
    /// </summary>
    /// <param name="employee">The recipient employee details.</param>
    /// <param name="license">The license details nearing expiration.</param>
    public void SendExpirationNotice(Employee employee, License license, bool isExpired)
    {
        // Decode Base64 password for authentication
        string decodedPassword = Encoding.UTF8.GetString(Convert.FromBase64String(_settings.MailServer.Password));

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Gestione Patenti", $"noreply@vigilfuoco.it"));
        message.To.Add(new MailboxAddress($"{employee.FirstName} {employee.LastName}", employee.Mail));

        // Add BCC recipients from configuration array
        if (_settings.MailBcc != null)
        {
            foreach (var bcc in _settings.MailBcc)
            {
                if (MailboxAddress.TryParse(bcc, out var bccAddress)) message.Bcc.Add(bccAddress);
            }
        }
                
        message.Subject = isExpired
            ? $"ATTENZIONE: Patente SCADUTA - {employee.LastName} {employee.FirstName}"
            : $"Promemoria: Patente in scadenza - {employee.LastName} {employee.FirstName}";

        var statusMessage = isExpired
            ? $"La tua patente è <b>SCADUTA</b><br/>"
            : $"<p>La tua patente di <b>{license.Category}</b> scadrà il <b>{license.ExpiryDate:d}</b>.</p>";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
            <h3>Avviso Scadenza Documenti</h3>
            <p>Buongiorno {employee.FirstName} {employee.LastName}</p>
            {statusMessage}
            <br/>
            <small>***La presente mail è generata automaticamente dal sistema.Per qualsiasi comunicazione, si prega di non rispondere a questa mail, ma di contattare l'help desk tecnico ***</small>"
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            // Connect using SMTPS (465)
            client.Connect(_settings.MailServer.Host, 465, SecureSocketOptions.SslOnConnect);

            // Authenticate using decoded credentials
            client.Authenticate(_settings.MailServer.Username, decodedPassword);

            // Transmit the message
            client.Send(message);

            // Gracefully disconnect from the server
            client.Disconnect(true);

            _logger.LogInformation("Email successfully sent to {Email} regarding license expiring on {Expiry}",
                employee.Mail, license.ExpiryDate.ToShortDateString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP protocol error while sending notification to {Email}", employee.Mail);
            // Re-throw to allow the orchestrator to handle the failure (e.g., logging or retry)
            throw;
        }
    }

    /// <summary>
    /// Verifies SMTP connectivity and sends a test email.
    /// Follows the Fail-Fast principle.
    /// </summary>
    public bool VerifyEmailConnectivity()
    {
        // Decode Base64 password for authentication
        string decodedPassword = Encoding.UTF8.GetString(Convert.FromBase64String(_settings.MailServer.Password));

        _logger.LogInformation("MailKit: Starting connection test to {Host}:465...", _settings.MailServer.Host);

        // MimeMessage is the MailKit equivalent of MailMessage
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Gestione Patenti", $"noreply@vigilfuoco.it"));
        message.To.Add(new MailboxAddress("Admin", _settings.AdminEmail ?? string.Empty));
        message.Subject = "License-Reminder: MailKit Health Check";

        message.Body = new TextPart("plain")
        {
            Text = $"Health check performed at {DateTime.Now}. MailKit is working correctly."
        };

        using var client = new SmtpClient();

        try
        {
            // Connect to the server
            // SecureSocketOptions.Auto allows MailKit to decide the best SSL/TLS strategy
            client.Connect(_settings.MailServer.Host, 465, SecureSocketOptions.Auto);

            // Authenticate if credentials are provided
            if (!string.IsNullOrEmpty(_settings.MailServer.Username))
            {
                client.Authenticate(_settings.MailServer.Username, decodedPassword);
            }

            client.Send(message);
            client.Disconnect(true);

            _logger.LogInformation("MailKit: Health Check SUCCESS. Email delivered.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MailKit: Health Check FAILED. Error: {Message}", ex.Message);
            return false;
        }
    }
}