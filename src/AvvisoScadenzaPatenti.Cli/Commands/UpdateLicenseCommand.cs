namespace AvvisoScadenzaPatenti.Cli.Commands;

using AvvisoScadenzaPatenti.Core.Interfaces;
using Serilog;
using System.Globalization;

public class UpdateLicenseCommand : ILicenseCommand
{
    private readonly ILicenseRepository _licenseRepo;

    public UpdateLicenseCommand(ILicenseRepository licenseRepo)
    {
        _licenseRepo = licenseRepo;
    }

    public Task<int> ExecuteAsync(Options opts, CancellationToken ct)
    {
        if (!DateTime.TryParseExact(
                opts.NewExpiryDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var newExpiryDate))
        {
            Console.WriteLine($"Invalid date format: '{opts.NewExpiryDate}'. Usa yyyy-MM-dd.");
            return Task.FromResult(1);
        }

        var license = _licenseRepo.GetByLicenseNumber(opts.UpdateLicenseNumber!);

        if (license is null)
        {
            Log.Warning("No license found with number {LicenseNumber}.", opts.UpdateLicenseNumber);
            return Task.FromResult(1);
        }

        license.ExpiryDate = newExpiryDate;
        _licenseRepo.SaveAll(_licenseRepo.GetAll());

        Log.Information(
            "Driving licence {LicenseNumber} updated: new expiration date {ExpiryDate:yyyy-MM-dd}.",
            opts.UpdateLicenseNumber,
            newExpiryDate);

        return Task.FromResult(0);
    }
}