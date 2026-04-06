namespace AvvisoScadenzaPatenti.Cli;

using CommandLine;

/// <summary>
/// Command-line options for the AvvisoScadenzaPatenti CLI application.
/// Defines the arguments that can be passed on the command line, such as:
/// - Encrypting a password and saving it to configuration.
/// - Controlling batch processing behavior (when added).
/// </summary>
public class Options
{
    /// <summary>
    /// Plain text password to be encrypted and saved to appsettings.json.
    /// If specified, the program encrypts this password and exits without further processing.
    /// Example: --password-to-crypt "mypass"
    /// </summary>
    [Option(
        "password-to-crypt",
        Required = false,
        HelpText = "Encrypt and save this password into appsettings.json under Settings.MailServer.Password.")]
    public string? PasswordToCrypt { get; set; }
}