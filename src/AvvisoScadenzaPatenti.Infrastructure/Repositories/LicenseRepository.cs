namespace AvvisoScadenzaPatenti.Infrastructure.Repositories;

using System.Globalization;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Mappings;
using AvvisoScadenzaPatenti.Core.Entities;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of ILicenseRepository using CsvHelper for flat-file storage.
/// Provides read-only access to license data stored in a CSV file.
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
    /// <param name="logger">Logger instance for diagnostic and error messages.</param>
    public LicenseRepository(string filePath, ILogger<LicenseRepository> logger)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,

            MissingFieldFound = args =>
            {
                var headerName = args.HeaderNames?.FirstOrDefault() ?? "Unknown Column";
                _logger.LogWarning(
                    "Missing field in CSV: {HeaderName} at index {Index}",
                    headerName,
                    args.Index);
            },

            HeaderValidated = args =>
            {
                if (args.InvalidHeaders.Any())
                {
                    _logger.LogError("Invalid CSV headers detected.");
                }
            }
        };
    }

    /// <summary>
    /// Reads all license records from the CSV file.
    /// If the file does not exist, returns an empty collection.
    /// </summary>
    /// <returns>A list of licenses read from the CSV file.</returns>
    public IEnumerable<License> GetAll()
    {
        // Use the cache if already populated (Singleton-like pattern)
        if (_cache?.Count > 0) return _cache;

        _logger.LogInformation("Loading licenses from {Path}", _filePath);

        if (!File.Exists(_filePath))
        {
            _logger.LogWarning("CSV file not found. Returning empty collection.");
            return Enumerable.Empty<License>();
        }

        using var reader = new StreamReader(_filePath);
        using var csv = new CsvReader(reader, _csvConfig);

        csv.Context.RegisterClassMap<LicenseMap>();

        return csv.GetRecords<License>().ToList();
    }

    /// <summary>
    /// Retrieves a license by its license number (case-insensitive search).
    /// </summary>
    /// <param name="licenseNumber">The license number to search for.</param>
    /// <returns>The matching license if found; otherwise null.</returns>
    public License? GetByLicenseNumber(string licenseNumber)
    {
        var licenses = this.GetAll();

        return licenses.FirstOrDefault(e =>
            string.Equals(e.LicenseNumber, licenseNumber, StringComparison.OrdinalIgnoreCase));
    }

    public void SaveAll(IEnumerable<License> licenses)
    {
        _cache = licenses.ToList();
        this.SaveChanges();
    }

    /// <summary>
    /// Writes the current cache of licenses back to the CSV file, overwriting it.
    /// Uses the class-wide CSV configuration for header and field handling.
    /// Logs any error but does not swallow the exception.
    /// </summary>
    private void SaveChanges()
    {
        try
        {
            using var writer = new StreamWriter(_filePath);
            using var csv = new CsvWriter(writer, _csvConfig);

            // Register the mapping here so CsvReader knows how to bind columns to properties
            csv.Context.RegisterClassMap<LicenseMap>();

            // Now the compiler knows _cache is not null because of the check above
            csv.WriteRecords(_cache);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write employees to CSV at {Path}", _filePath);
            throw;
        }
    }
}