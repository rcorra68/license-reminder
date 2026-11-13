namespace AvvisoScadenzaPatenti.Cli.Commands;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Shared;
using Serilog;

/// <summary>
/// Marks a driving licence as destroyed (STATO = PATENTE DISTRUTTA), e.g. when the holder retires.
/// The record is kept in licenses.csv for traceability but is excluded from expiration handling.
/// </summary>
public class DestroyLicenseCommand : ILicenseCommand
{
    private readonly ILicenseRepository _licenseRepo;

    public DestroyLicenseCommand(ILicenseRepository licenseRepo)
    {
        _licenseRepo = licenseRepo;
    }

    public Task<int> ExecuteAsync(Options opts, CancellationToken ct)
    {
        var licenseNumber = opts.DestroyLicenseNumber!.Trim();
        var license = _licenseRepo.GetByLicenseNumber(licenseNumber);

        if (license is null)
        {
            Log.Warning("No license found with number {LicenseNumber}.", licenseNumber);
            return Task.FromResult(1);
        }

        if (license.IsDestroyed())
        {
            Console.WriteLine($"License n. {license.LicenseNumber} ({license.LastName} {license.FirstName}) is already marked as destroyed.");
            return Task.FromResult(0);
        }

        var previousStatus = license.Status;
        license.Status = LicenseStatus.Destroyed;
        _licenseRepo.SaveAll(_licenseRepo.GetAll());

        Log.Information(
            "Driving licence {LicenseNumber} ({LastName} {FirstName}) marked as destroyed (previous status: {PreviousStatus}).",
            license.LicenseNumber,
            license.LastName,
            license.FirstName,
            previousStatus);

        return Task.FromResult(0);
    }
}
