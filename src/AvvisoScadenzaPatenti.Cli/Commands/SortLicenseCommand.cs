namespace AvvisoScadenzaPatenti.Cli.Commands;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Shared.Sorting;

public class SortLicensesCommand : ILicenseCommand
{
    private readonly ILicenseRepository _licenseRepo;

    public SortLicensesCommand(ILicenseRepository licenseRepo)
    {
        _licenseRepo = licenseRepo;
    }

    public Task<int> ExecuteAsync(Options opts, CancellationToken ct)
    {
        var licenses = _licenseRepo.GetAll();
        var sorted = LicenseSorting.Sort(licenses, opts.SortBy!.Value, opts.SortOrder);
        _licenseRepo.SaveAll(sorted);

        return Task.FromResult(0);
    }
}