namespace AvvisoScadenzaPatenti.Cli;

using AvvisoScadenzaPatenti.Core.Enums;

using CommandLine;

public class Options
{
    [Option("init", Required = false,
        HelpText = "Initialize an empty appsettings.json file.")]
    public bool Init { get; set; }

    [Option('f', "force", Required = false,
        HelpText = "Overwrite appsettings.json if it already exists.")]
    public bool Force { get; set; }

    [Option("sort-by", Required = false,
        HelpText = "Field used to sort the CSV (e.g. Name, ExpiryDate, ReleaseDate). Sorting by ReleaseDate (Asc) surfaces licenses still missing a release date.")]
    public CsvSortField? SortBy { get; set; }

    [Option("sort-order", Required = false, Default = CsvSortOrder.Asc,
        HelpText = "Sort order: asc or desc.")]
    public CsvSortOrder SortOrder { get; set; }

    [Option("update-license", Required = false,
        HelpText = "Driving licence number to be updated.")]
    public string? UpdateLicenseNumber { get; set; }

    [Option("new-expiry-date", Required = false,
        HelpText = "New expiration date (format yyyy-MM-dd).")]
    public string? NewExpiryDate { get; set; }

    [Option("name", Required = false,
        HelpText = "Free search text: first name, last name, part of one or both, in any order.")]
    public string? Name { get; set; }

    [Option("upcoming-expirations", Required = false,
        HelpText = "Show the N soonest-expiring licenses, including already expired ones. Usage: --upcoming-expirations [N]")]
    public IEnumerable<int>? UpcomingExpirations { get; set; }

    [Option("match-cf", Required = false,
        HelpText = "Fiscal code to match against employees.csv (by decoded surname/name code) to enrich the matching record with fiscal code and birth date.")]
    public string? MatchCf { get; set; }

    [Option("resolve-index", Required = false,
        HelpText = "When --match-cf finds multiple homonym candidates, selects which one to update (1-based index from the printed list).")]
    public int? ResolveIndex { get; set; }
}