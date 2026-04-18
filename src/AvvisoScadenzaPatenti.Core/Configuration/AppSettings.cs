namespace AvvisoScadenzaPatenti.Core.Configuration;

using System.ComponentModel.DataAnnotations;

public class AppSettings
{
    public string[] MailBcc { get; set; } = [];
    public string AdminEmail { get; set; } = "";

    [Required]
    public SmtpSettings Smtp { get; set; } = new();
}