namespace AvvisoScadenzaPatenti.Infrastructure.Repositories;

using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Extensions.Logging;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Models;

/// <summary>
/// Implementation of IEmployeeRepository using CsvHelper for flat-file storage.
/// Employs an in-memory cache to avoid repeatedly reading the CSV file.
/// </summary>
public class EmployeeRepository : IEmployeeRepository
{
    private readonly string _filePath;
    private readonly ILogger<EmployeeRepository> _logger;
    private readonly CsvConfiguration _csvConfig;
    private List<Employee>? _cache;

    /// <summary>
    /// Initializes a new instance of the EmployeeRepository.
    /// </summary>
    /// <param name="filePath">Path to the CSV file containing employee data.</param>
    /// <param name="logger">Logger instance for diagnostic messages.</param>
    public EmployeeRepository(string filePath, ILogger<EmployeeRepository> logger)
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
    /// Loads all employee records from the CSV file, caching them for subsequent calls.
    /// If the cache is already populated, returns the cached list without re‑reading the file.
    /// If the file does not exist, returns an empty list.
    /// </summary>
    /// <returns>A list of all employees.</returns>
    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        // Use the cache if already populated (Singleton-like pattern)
        if (_cache != null) return _cache;

        _logger.LogInformation("Cache empty. Loading employees from {Path}", _filePath);

        // Check if file exists to avoid FileNotFoundException
        if (!File.Exists(_filePath))
        {
            _logger.LogWarning("CSV file not found. Starting with an empty list.");
            _cache = new List<Employee>();
            return _cache;
        }

        using var reader = new StreamReader(_filePath);
        using var csv = new CsvReader(reader, _csvConfig); // Use the class config!

        // Load everything into memory once
        _cache = csv.GetRecords<Employee>().ToList();

        return _cache;
    }

    /// <summary>
    /// Finds an employee by email address, ignoring case.
    /// This will trigger a load from the CSV file if the cache is not yet initialized.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <returns>The matching employee, or null if not found.</returns>
    public async Task<Employee?> GetByEmailAsync(string email)
    {
        // This will trigger GetAllAsync only if _cache is null
        var employees = await GetAllAsync();
        return employees.FirstOrDefault(e =>
            e.Mail.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds an employee by their lastName and firstName (case-insensitive).
    /// This will trigger a load from the CSV file if the cache is not yet initialized.
    /// </summary>
    /// <param name="lastName">The last name to search for.</param>
    /// <param name="firstName">The first name to search for.</param>
    /// <returns>The matching employee, or null if not found.</returns>
    public async Task<Employee?> GetByNameAsync(string lastName, string firstName)
    {
        // Ensure the cache is populated before searching
        var employees = await GetAllAsync();

        // Perform a case-insensitive search on both fields
        return employees.FirstOrDefault(e => 
            e.LastName.Equals(lastName, StringComparison.OrdinalIgnoreCase) && 
            e.FirstName.Equals(firstName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Adds a new employee to the repository and persists the changes to the CSV file.
    /// Ensures the cache is loaded before adding the record.
    /// </summary>
    /// <param name="employee">The employee to add. Must not be null.</param>
    public async Task AddAsync(Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        // Ensure cache is initialized from file
        await GetAllAsync();

        _cache!.Add(employee);

        // Persist the updated cache to disk
        await SaveChangesAsync();
    }

    /// <summary>
    /// Writes the current cache of employees back to the CSV file, overwriting it.
    /// Uses the class-wide CSV configuration for header and field handling.
    /// Logs any error but does not swallow the exception.
    /// </summary>
    private async Task SaveChangesAsync()
    {
        // Safety check: if cache is null, we have nothing to save
        if (_cache == null)
        {
            _logger.LogWarning("Attempted to save an uninitialized cache. Skipping write.");
            return;
        }

        try 
        {
            using var writer = new StreamWriter(_filePath);
            using var csv = new CsvWriter(writer, _csvConfig);
            
            // Now the compiler knows _cache is not null because of the check above
            await csv.WriteRecordsAsync(_cache); 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write employees to CSV at {Path}", _filePath);
            throw;
        }
    }
}