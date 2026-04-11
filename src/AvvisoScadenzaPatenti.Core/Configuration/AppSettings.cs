namespace AvvisoScadenzaPatenti.Core.Configuration;

public class AppSettings
{
    public string[] MailBcc { get; set; } = [];
    public string AdminEmail { get; set; } = "";
    public SmtpSettings Smtp { get; set; } = new();
}