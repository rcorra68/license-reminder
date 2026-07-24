namespace AvvisoScadenzaPatenti.Cli.Commands;

using AvvisoScadenzaPatenti.Core.Entities;
using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Shared.FiscalCode;
using Serilog;

public class MatchFiscalCodeCommand : ILicenseCommand
{
    private readonly IEmployeeRepository _employeeRepo;

    public MatchFiscalCodeCommand(IEmployeeRepository employeeRepo)
    {
        _employeeRepo = employeeRepo;
    }

    public Task<int> ExecuteAsync(Options opts, CancellationToken ct)
    {
        var cf = opts.MatchCf!.Trim().ToUpperInvariant();

        if (cf.Length < 11)
        {
            Console.WriteLine($"Invalid fiscal code: '{opts.MatchCf}'. Expected at least 11 characters.");
            return Task.FromResult(1);
        }

        var birthDate = FiscalCodeDecoder.ExtractBirthDate(cf);
        var surnameCode = FiscalCodeDecoder.ExtractSurnameCode(cf);
        var nameCode = FiscalCodeDecoder.ExtractNameCode(cf);

        var candidates = _employeeRepo.GetAll()
            .Where(e =>
                FiscalCodeDecoder.ComputeSurnameCode(e.LastName) == surnameCode &&
                FiscalCodeDecoder.ComputeNameCode(e.FirstName) == nameCode)
            .ToList();

        if (candidates.Count == 0)
        {
            Console.WriteLine($"No employee found matching fiscal code '{opts.MatchCf}'.");
            return Task.FromResult(1);
        }

        Employee selected;

        if (candidates.Count == 1)
        {
            selected = candidates[0];
        }
        else if (opts.ResolveIndex is null)
        {
            Console.WriteLine($"Found {candidates.Count} homonym candidates matching fiscal code '{opts.MatchCf}':");

            for (var i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                var currentBirthText = c.BirthDate is { } bd ? bd.ToString("dd/MM/yyyy") : "n/d";
                Console.WriteLine($"  [{i + 1}] {c.LastName} {c.FirstName} — {c.Mail} — born on {currentBirthText}");
            }

            Console.WriteLine("Use --match-cf <CF> --resolve-index <N> to pick which one to update.");

            return Task.FromResult(1);
        }
        else
        {
            var index = opts.ResolveIndex.Value - 1;

            if (index < 0 || index >= candidates.Count)
            {
                Console.WriteLine($"Invalid --resolve-index {opts.ResolveIndex}. Must be between 1 and {candidates.Count}.");
                return Task.FromResult(1);
            }

            selected = candidates[index];
        }

        selected.FiscalCode = cf;
        selected.BirthDate = birthDate;
        _employeeRepo.Update(selected);

        Log.Information(
            "Employee {LastName} {FirstName} updated with fiscal code {FiscalCode} and birth date {BirthDate:yyyy-MM-dd}.",
            selected.LastName,
            selected.FirstName,
            selected.FiscalCode,
            selected.BirthDate);

        return Task.FromResult(0);
    }
}