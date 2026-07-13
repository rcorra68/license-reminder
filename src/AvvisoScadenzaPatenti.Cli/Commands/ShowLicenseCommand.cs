namespace AvvisoScadenzaPatenti.Cli.Commands;

using AvvisoScadenzaPatenti.Core.Interfaces;
using Serilog;

public class ShowLicenseCommand : ILicenseCommand
{
    private readonly ILicenseRepository _licenseRepo;

    public ShowLicenseCommand(ILicenseRepository licenseRepo)
    {
        _licenseRepo = licenseRepo;
    }

    public Task<int> ExecuteAsync(Options opts, CancellationToken ct)
    {
        var license = _licenseRepo.GetByLicenseNumber(opts.UpdateLicenseNumber!);

        if (license is null)
        {
            Log.Warning("No license found with number {LicenseNumber}.", opts.UpdateLicenseNumber);
            return Task.FromResult(1);
        }

        Console.WriteLine($"{license.FirstName} {license.LastName} — license n. {license.LicenseNumber}");
        Console.WriteLine($"Current expiration: {license.ExpiryDate:yyyy-MM-dd}");

        return Task.FromResult(0);
    }
}