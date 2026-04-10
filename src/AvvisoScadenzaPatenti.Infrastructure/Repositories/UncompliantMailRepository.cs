namespace AvvisoScadenzaPatenti.Infrastructure.Repositories;

using System.Globalization;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Mappings;
using AvvisoScadenzaPatenti.Core.Models;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of UncompliantMailRepository using CsvHelper for flat-file storage.
/// </summary>
public class UncompliantMailRepository : IUncompliantMailRepository
{
    private readonly string _filePath;
    private readonly ILogger<UncompliantMailRepository> _logger;
    private readonly CsvConfiguration _csvConfig;
    private List<UncompliantMail> _cache = [];

    /// <summary>
    /// Initializes a new instance of the UncompliantMailRepository.
    /// </summary>
    /// <param name="filePath">Path to the CSV file containing uncompliant mail data.</param>
    /// <param name="logger">Logger instance for diagnostic messages.</param>
    public UncompliantMailRepository(string filePath, ILogger<UncompliantMailRepository> logger)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            // Let's implement logging for missing fields instead of just ignoring them
            MissingFieldFound = args =>
            {
                _logger.LogWarning("Missing field in CSV: {HeaderName} at index {Index}",
                    args.HeaderNames!.FirstOrDefault(), args.Index);
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
    /// Loads all uncompliant mail records from the CSV file, caching them for subsequent calls.
    /// If the cache is already populated, returns the cached list without re‑reading the file.
    /// </summary>
    /// <returns>A list of all uncompliant mail records.</returns>
    public IEnumerable<UncompliantMail> GetAll()
    {
        _logger.LogInformation("Cache empty. Loading uncompliant mails from {Path}", _filePath);

        // Check if file exists to avoid FileNotFoundException
        if (!File.Exists(_filePath))
        {
            _logger.LogWarning("CSV file not found. Starting with an empty list.");
            _cache = new List<UncompliantMail>();
            return _cache;
        }

        using var reader = new StreamReader(_filePath);
        using var csv = new CsvReader(reader, _csvConfig); // Use the class config!

        // Register the mapping here so CsvReader knows how to bind columns to properties
        csv.Context.RegisterClassMap<UncompliantMailMap>();

        // Load everything into memory once
        _cache = csv.GetRecords<UncompliantMail>().ToList();

        return _cache;
    }

    /// <summary>
    /// Finds an uncompliant mail record based on the first and last name.
    /// Returns the full UncompliantMail object if found, otherwise null.
    /// </summary>
    /// <param name="firstName">The first name to search for.</param>
    /// <param name="lastName">The last name to search for.</param>
    /// <returns>The matching UncompliantMail record, or null if not found.</returns>
    public UncompliantMail? GetByName(string firstName, string lastName)
    {
        // 1. Ensure the cache is populated by calling the base retrieval method
        var uncompliants = GetAll();

        // 2. Search in the returned collection (safer than accessing _cache directly)
        // Using OrdinalIgnoreCase to avoid issues with different casing in CSV
        return uncompliants.FirstOrDefault(u =>
            string.Equals(u.LastName, lastName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(u.FirstName, firstName, StringComparison.OrdinalIgnoreCase));
    }
}