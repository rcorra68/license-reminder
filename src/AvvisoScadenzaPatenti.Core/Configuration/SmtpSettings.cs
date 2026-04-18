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
    
    public SmtpSecurityMode SecurityMode { get; set; } = SmtpSecurityMode.StartTls;
}
