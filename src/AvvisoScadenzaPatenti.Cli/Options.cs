namespace AvvisoScadenzaPatenti.Cli;

using AvvisoScadenzaPatenti.Core.Enums;

using CommandLine;

/// <summary>
/// Command-line options for the AvvisoScadenzaPatenti CLI application.
/// Defines the arguments that can be passed on the command line, such as:
/// - Encrypting a password and saving it to configuration.
/// - Controlling batch processing behavior (when added).
/// </summary>
public class Options
{
    /// <summary>
    /// Initialize an empty appsettings.json file.
    /// </summary>
    [Option("init", Required = false, HelpText = "Initialize an empty appsettings.json file.")]
    public bool Init { get; set; }

    /// <summary>
    /// Overwrite appsettings.json if it already exists.
    /// </summary>
    [Option('f', "force", Required = false, HelpText = "Overwrite appsettings.json if it already exists.")]
    public bool Force { get; set; }

    [Option("sort-by", Required = false, HelpText = "Field used to sort the CSV (e.g. Name, ExpiryDate, ReleaseDate). Sorting by ReleaseDate (Asc) surfaces licenses still missing a release date.")]
    public CsvSortField? SortBy { get; set; }

    [Option("sort-order", Required = false, Default = CsvSortOrder.Asc, HelpText = "Sort order: asc or desc.")]
    public CsvSortOrder SortOrder { get; set; }

    [Option("update-license", Required = false, HelpText = "Driving licence number to be updated.")]
    public string? UpdateLicenseNumber { get; set; }

    [Option("new-expiry-date", Required = false, HelpText = "New expiration date (format yyyy-MM-dd).")]
    public string? NewExpiryDate { get; set; }

    [Option("name", Required = false, HelpText = "Free search text: first name, last name, part of one or both, in any order.")]
    public string? Name { get; set; }
}