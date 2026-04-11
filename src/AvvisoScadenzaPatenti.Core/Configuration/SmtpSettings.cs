namespace AvvisoScadenzaPatenti.Core.Configuration;

using AvvisoScadenzaPatenti.Core.Models;
public class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public SmtpSecurityMode SecurityMode { get; set; } = SmtpSecurityMode.StartTls;
}
