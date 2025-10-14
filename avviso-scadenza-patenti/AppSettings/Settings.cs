namespace avviso_scadenza_patenti.AppSettings
{
    internal class Settings
    {
        public const string SectionName = "Settings";
        public List<string>? MailBcc { get; set; }
        public MailServer MailServer { get; set; }
    }
}
