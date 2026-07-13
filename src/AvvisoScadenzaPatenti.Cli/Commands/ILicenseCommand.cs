namespace AvvisoScadenzaPatenti.Cli.Commands;

public interface ILicenseCommand
{
    Task<int> ExecuteAsync(Options opts, CancellationToken ct);
}