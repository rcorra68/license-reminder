namespace AvvisoScadenzaPatenti.Core.Configuration;

public record AppSettings
{
    public List<string> MailBcc { get; init; } = new();
    public string AdminEmail { get; init; } = string.Empty;
    public MailServerSettings MailServer { get; init; } = new();
}