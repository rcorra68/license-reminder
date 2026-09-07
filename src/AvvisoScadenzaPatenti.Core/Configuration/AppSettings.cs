namespace AvvisoScadenzaPatenti.Core.Configuration;

using System.ComponentModel.DataAnnotations;

public class AppSettings
{
    public string MailBcc { get; set; } = "";
    public string AdminEmail { get; set; } = "";

    [Required]
    public SmtpSettings Smtp { get; set; } = new();

    [Required]
    public NotificationSettings Notification { get; set; } = new();

    public IReadOnlyList<string> MailBccAddresses =>
        MailBcc.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}