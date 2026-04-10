namespace AvvisoScadenzaPatenti.Infrastructure.Repositories;

using System.Globalization;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Mappings;
using AvvisoScadenzaPatenti.Core.Models;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of LicenseRepository using CsvHelper for flat-file storage.
/// Employs an in-memory cache to avoid repeatedly reading the CSV file.
/// </summary>
public class LicenseRepository : ILicenseRepository
{
    private readonly string _filePath;
    private readonly ILogger<LicenseRepository> _logger;
    private readonly CsvConfiguration _csvConfig;
    private List<License> _cache = [];

    /// <summary>
    /// Initializes a new instance of the LicenseRepository.
    /// </summary>
    /// <param name="filePath">Path to the CSV file containing license data.</param>
    /// <param name="logger">Logger instance for diagnostic messages.</param>
    public LicenseRepository(string filePath, ILogger<LicenseRepository> logger)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            // Let's implement logging for missing fields instead of just ignoring them
            MissingFieldFound = args =>
            {
                // Use '?' to safely handle the potential null 'HeaderNames'
                var headerName = args.HeaderNames?.FirstOrDefault() ?? "Unknown Column";
                _logger.LogWarning("Missing field in CSV: {HeaderName} at index {Index}",  headerName, args.Index);
            },
            HeaderValidated = args =>
            {
                if (args.InvalidHeaders.Any())
                {
                    _logger.LogError("Invalid CSV Headers detected.");
                }
            }
        };
    }

    /// <summary>
    /// Loads all license records from the CSV file, caching them for subsequent calls.
    /// If the cache is already populated, returns the cached list without re‑reading the file.
    /// Skips the file existence check assuming it is created by the application or provided upfront.
    /// </summary>
    /// <returns>A list of all licenses.</returns>
    public IEnumerable<License> GetAll()
    {
        if (_cache != null) return _cache;

        using var reader = new StreamReader(_filePath);
        using var csv = new CsvReader(reader, _csvConfig); // Use the class-wide configuration

        // Register the mapping here so CsvReader knows how to bind columns to properties
        csv.Context.RegisterClassMap<LicenseMap>();

        var records = csv.GetRecords<License>().ToList();
        _cache = records;

        return _cache;
    }

    public License? GetByLicenseNumber(string licenseNumber)
    {
        // Ensure the cache is populated before searching
        var licenses = GetAll();

        // Perform a case-insensitive search on both fields
        return licenses.FirstOrDefault(e =>
            e.LicenseNumber.Equals(licenseNumber, StringComparison.OrdinalIgnoreCase));
    }
}