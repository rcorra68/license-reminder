namespace AvvisoScadenzaPatenti.Cli;

using CommandLine;
public class Options
{
    [Option('c', "crypt", Required = false, HelpText = "Encrypt a plain-text password and save it to appsettings.json.")]
    public string? PasswordToCrypt { get; set; }
}
