namespace AvvisoScadenzaPatenti.Cli;

using CommandLine;
public class Options
{
    [Option('c', "crypt", Required = false, HelpText = "Encrypts the provided plain-text password for secure storage.")]
    public string? PasswordToEncrypt { get; set; }
}
