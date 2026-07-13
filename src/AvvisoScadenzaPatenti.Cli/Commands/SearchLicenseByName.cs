namespace AvvisoScadenzaPatenti.Cli.Commands;

using AvvisoScadenzaPatenti.Core.Interfaces;
using Serilog;

public class SearchLicenseByNameCommand : ILicenseCommand
{
    private readonly ILicenseRepository _licenseRepo;
    private readonly IEmployeeRepository _employeeRepo;

    public SearchLicenseByNameCommand(ILicenseRepository licenseRepo, IEmployeeRepository employeeRepo)
    {
        _licenseRepo = licenseRepo;
        _employeeRepo = employeeRepo;
    }

    public Task<int> ExecuteAsync(Options opts, CancellationToken ct)
    {
        var matches = _licenseRepo.SearchByName(opts.Name!).ToList();

        if (matches.Count == 0)
        {
            Log.Warning("No licenses found for '{Query}'.", opts.Name);
            return Task.FromResult(1);
        }

        foreach (var license in matches)
        {
            var employee = _employeeRepo.GetByName(license.FirstName, license.LastName);
            var birthDateText = employee?.BirthDate is { } bd ? bd.ToString("dd/MM/yyyy") : "n/d";

            Console.WriteLine($"{license.FirstName} {license.LastName} — born on {birthDateText}");
            Console.WriteLine($"  License No. {license.LicenseNumber} — Current expiration date: {license.ExpiryDate:yyyy-MM-dd}");
            Console.WriteLine();
        }

        if (matches.Count > 1)
        {
            Console.WriteLine($"Found {matches.Count} licenses matching '{opts.Name}'. Use --update-license <number> to update the correct one.");
        }

        return Task.FromResult(0);
    }
}