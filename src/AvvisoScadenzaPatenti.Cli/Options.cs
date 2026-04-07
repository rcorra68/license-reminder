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
    /// Encrypt and save this password into appsettings.json under Settings.MailServer.Password.
    /// </summary>
    [Option('c', "crypt", Required = false, HelpText = "Encrypt and save this password into appsettings.json under Settings.MailServer.Password.")]
    public string? PasswordToCrypt { get; set; }

    /// <summary>
    /// Initialize an empty appsettings.json file.
    /// </summary>
    [Option("init", Required = false, HelpText = "Initialize an empty appsettings.json file.")]
    public bool Init { get; set; }

    /// <summary>
    /// Overwrite appsettings.json if it already exists.
    /// </summary>
    [Option('f', "force", Required = false, HelpText = "Overwrite appsettings.json if it already exists.")]
    public bool Force { get; set; }
}