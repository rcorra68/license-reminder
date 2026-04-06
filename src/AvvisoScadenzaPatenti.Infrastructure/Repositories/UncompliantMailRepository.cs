namespace AvvisoScadenzaPatenti.Infrastructure.Repositories;

using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Extensions.Logging;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Models;

/// <summary>
/// Implementation of UncompliantMailRepository using CsvHelper for flat-file storage.
/// </summary>
public class UncompliantMailRepository : IUncompliantMailRepository
{
    private readonly string _filePath;
    private readonly ILogger<UncompliantMailRepository> _logger;
    private readonly CsvConfiguration _csvConfig;
    private List<UncompliantMail>? _cache;

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
    public async Task<IEnumerable<UncompliantMail>> GetAllAsync()
    {
        if (_cache != null) return _cache;

        using var reader = new StreamReader(_filePath);
        using var csv = new CsvReader(reader, _csvConfig); // uses the class-wide configuration
        _cache = csv.GetRecords<UncompliantMail>().ToList();

        return _cache;
    }

    /// <summary>
    /// Finds an employee information based on the first and last name in the uncompliant mail records.
    /// If a matching record is found, returns a minimal Employee object containing only the email.
    /// If no record is found, returns null.
    /// </summary>
    /// <param name="lastName">The last name to search for.</param>
    /// <param name="firstName">The first name to search for.</param>
    /// <returns>An Employee with the email from the uncompliant record, or null if not found.</returns>
    public async Task<Employee?> GetByNameAsync(string lastName, string firstName)
    {
        var uncompliant = _cache?.FirstOrDefault(u => u.LastName == lastName && u.FirstName == firstName);
        return uncompliant != null
            ? new Employee { LastName = lastName, FirstName = firstName, Mail = uncompliant.Mail! }
            : null;
    }
}