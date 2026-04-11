namespace AvvisoScadenzaPatenti.Infrastructure.Repositories;

using System.Globalization;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Mappings;
using AvvisoScadenzaPatenti.Core.Entities;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of IUncompliantMailRepository using CsvHelper for flat-file storage.
/// Provides read-only access to uncompliant mail records stored in a CSV file.
/// </summary>
public class UncompliantMailRepository : IUncompliantMailRepository
{
    private readonly string _filePath;
    private readonly ILogger<UncompliantMailRepository> _logger;
    private readonly CsvConfiguration _csvConfig;

    /// <summary>
    /// Initializes a new instance of the UncompliantMailRepository.
    /// </summary>
    /// <param name="filePath">Path to the CSV file containing uncompliant mail data.</param>
    /// <param name="logger">Logger instance for diagnostic and error messages.</param>
    public UncompliantMailRepository(string filePath, ILogger<UncompliantMailRepository> logger)
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
    /// Reads all uncompliant mail records from the CSV file.
    /// If the file does not exist, returns an empty collection.
    /// </summary>
    /// <returns>A list of uncompliant mail records.</returns>
    public IEnumerable<UncompliantMail> GetAll()
    {
        _logger.LogInformation("Loading uncompliant mails from {Path}", _filePath);

        if (!File.Exists(_filePath))
        {
            _logger.LogWarning("CSV file not found. Returning empty collection.");
            return Enumerable.Empty<UncompliantMail>();
        }

        using var reader = new StreamReader(_filePath);
        using var csv = new CsvReader(reader, _csvConfig);

        csv.Context.RegisterClassMap<UncompliantMailMap>();

        return csv.GetRecords<UncompliantMail>().ToList();
    }

    /// <summary>
    /// Retrieves an uncompliant mail record by first and last name (case-insensitive search).
    /// </summary>
    /// <param name="firstName">The first name to search for.</param>
    /// <param name="lastName">The last name to search for.</param>
    /// <returns>The matching record if found; otherwise null.</returns>
    public UncompliantMail? GetByName(string firstName, string lastName)
    {
        var uncompliants = GetAll();

        return uncompliants.FirstOrDefault(u =>
            string.Equals(u.LastName, lastName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(u.FirstName, firstName, StringComparison.OrdinalIgnoreCase));
    }
}