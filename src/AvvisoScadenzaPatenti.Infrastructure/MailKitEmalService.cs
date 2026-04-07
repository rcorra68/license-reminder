namespace AvvisoScadenzaPatenti.Infrastructure;

using System.Text;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Models;

using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using MimeKit;

public class MailKitEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<MailKitEmailService> _logger;

    public MailKitEmailService(IConfiguration config, ILogger<MailKitEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Sends an expiration notice email using SMTP settings from configuration.
    /// </summary>
    /// <param name="employee">The recipient employee details.</param>
    /// <param name="license">The license details nearing expiration.</param>
    public async Task SendExpirationNoticeAsync(Employee employee, License license, bool isExpired)
    {
        // Retrieve SMTP settings from appsettings.json
        var mailSettings = _config.GetSection("Settings:MailServer");
        string host = mailSettings["Host"] ?? throw new InvalidOperationException("SMTP Host is not configured.");
        string username = mailSettings["Username"] ?? string.Empty;
        string base64Password = mailSettings["Password"] ?? string.Empty;

        // Decode Base64 password for authentication
        string decodedPassword = Encoding.UTF8.GetString(Convert.FromBase64String(base64Password));

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("License Management System", username));
        message.To.Add(new MailboxAddress($"{employee.FirstName} {employee.LastName}", employee.Mail));

        // Add BCC recipients from configuration array
        var bccList = _config.GetSection("Settings:MailBcc").Get<string[]>();
        if (bccList != null)
        {
            foreach (var bcc in bccList)
            {
                if (MailboxAddress.TryParse(bcc, out var bccAddress))
                    message.Bcc.Add(bccAddress);
            }
        }
                
        message.Subject = isExpired
            ? $"ATTENZIONE: Patente SCADUTA - {employee.LastName} {employee.FirstName}"
            : $"Promemoria: Patente in scadenza - {employee.LastName} {employee.FirstName}";

        var bodyBuilder = new BodyBuilder();
        bodyBuilder.HtmlBody = $@"
            <h3>Avviso Scadenza Documenti</h3>
            <p>Buongiorno {employee.FirstName} {employee.LastName}</p>";

        bodyBuilder.HtmlBody = isExpired
            ? $"La tua patente è <b>SCADUTA</b><br/>"
            : $"<p>La tua patente di <b>{license.Category}</b> scadrà il/l'<b>{license.ExpiryDate:d}</b>.</p>";

        bodyBuilder.HtmlBody += $@"<p>Se hai già provveduto al rinnovo, ignora la presente mail. Altrimenti chiedi all'IIE ROBERTO CORRADETTI cosa fare per il rinnovo.</p>
            <br/>
            <small>***La presente mail è generata automaticamente dal sistema.Per qualsiasi comunicazione, si prega di non rispondere a questa mail, ma di contattare l'help desk tecnico ***</small>";

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            // Connect using SMTPS (Port 465)
            await client.ConnectAsync(host, 465, SecureSocketOptions.SslOnConnect);

            // Authenticate using decoded credentials
            await client.AuthenticateAsync(username, decodedPassword);

            // Transmit the message
            await client.SendAsync(message);

            // Gracefully disconnect from the server
            await client.DisconnectAsync(true);

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
}