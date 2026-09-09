namespace AvvisoScadenzaPatenti.Cli.Commands;

using AvvisoScadenzaPatenti.Core.Enums;
using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Shared.Sorting;

public class UpcomingExpirationsCommand : ILicenseCommand
{
    private readonly ILicenseRepository _licenseRepo;

    public UpcomingExpirationsCommand(ILicenseRepository licenseRepo)
    {
        _licenseRepo = licenseRepo;
    }

    public Task<int> ExecuteAsync(Options opts, CancellationToken ct)
    {
        var licenses = _licenseRepo.GetAll();

        var count = opts.UpcomingExpirations!.FirstOrDefault();
        if (count <= 0)
        {
            count = 5;
        }

        var upcoming = LicenseSorting
            .Sort(licenses, CsvSortField.ExpiryDate, CsvSortOrder.Asc)
            .Take(count);

        foreach (var license in upcoming)
        {
            var daysLeft = (license.ExpiryDate.Date - DateTime.Today).Days;
            var expiredMarker = daysLeft < 0 ? "[EXPIRED] " : string.Empty;
            Console.WriteLine($"{expiredMarker}{license.LastName} {license.FirstName} — expire date: {license.ExpiryDate:yyyy-MM-dd} - days left: {daysLeft}");
        }

        return Task.FromResult(0);
    }
}