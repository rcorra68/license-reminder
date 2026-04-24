namespace AvvisoScadenzaPatenti.Core.Configuration;

using System.ComponentModel.DataAnnotations;

using AvvisoScadenzaPatenti.Core.Models;
public class SmtpSettings
{
    [Required]
    public string Host { get; set; } = "";

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    // Proprietà calcolata: la logica di sicurezza è centralizzata qui
    public SmtpSecurityMode SecurityMode => this.Port switch
    {
        465 => SmtpSecurityMode.SslOnConnect,
        587 => SmtpSecurityMode.StartTls,
        25 or 1025 => SmtpSecurityMode.None,
        _ => SmtpSecurityMode.Auto
    };
}
