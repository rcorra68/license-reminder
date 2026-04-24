namespace AvvisoScadenzaPatenti.Infrastructure.Repositories;

using System.Globalization;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Mappings;
using AvvisoScadenzaPatenti.Core.Entities;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of IEmployeeRepository using CsvHelper for flat-file storage.
/// Employs an in-memory cache to avoid repeatedly reading the CSV file.
/// </summary>
public class EmployeeRepository : IEmployeeRepository
{
    private readonly string _filePath;
    private readonly ILogger<EmployeeRepository> _logger;
    private readonly CsvConfiguration _csvConfig;
    private List<Employee> _cache = [];

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
    public IEnumerable<Employee> GetAll()
    {
        // Use the cache if already populated (Singleton-like pattern)
        if (_cache?.Count > 0) return _cache;

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

        // Register the mapping here so CsvReader knows how to bind columns to properties
        csv.Context.RegisterClassMap<EmployeeMap>();

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
    public Employee? GetByEmail(string email)
    {
        var employees = GetAll();

        return employees.FirstOrDefault(e =>
            string.Equals(e.Mail, email, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds an employee by their lastName and firstName (case-insensitive).
    /// This will trigger a load from the CSV file if the cache is not yet initialized.
    /// </summary>
    /// <param name="firstName">The first name to search for.</param>
    /// <param name="lastName">The last name to search for.</param>
    /// <returns>The matching employee, or null if not found.</returns>
    public Employee? GetByName(string firstName, string lastName)
    {
        // Ensure the cache is populated before searching
        var employees = GetAll();

        // Perform a case-insensitive search on both fields
        return employees.FirstOrDefault(e =>
            e.LastName.Equals(lastName, StringComparison.OrdinalIgnoreCase) &&
            e.FirstName.Equals(firstName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Adds a new employee to the repository.
    /// After this call, the repository is responsible for persisting the change.
    /// </summary>
    /// <param name="employee">The employee to add. Must not be null.</param>
    public void Add(Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        if (string.IsNullOrWhiteSpace(employee.Mail))
            throw new ArgumentException("Employee email cannot be null or empty.", nameof(employee));

        // Ensure cache is initialized
        GetAll();

        // Check for duplicates (case-insensitive)
        bool exists = _cache.Any(e =>
            string.Equals(e.Mail, employee.Mail, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            _logger.LogWarning("Attempted to add duplicate employee with email {Email}", employee.Mail);
            throw new InvalidOperationException($"An employee with email '{employee.Mail}' already exists.");
        }

        _cache.Add(employee);

        _logger.LogDebug("Added employee {Email} to cache.", employee.Mail);

        SaveChanges();
    }

    /// <summary>
    /// Updates an existing employee in the repository.
    /// After this call, the repository is responsible for persisting the change.
    /// </summary>
    /// <param name="employee">The employee to update. Must not be null and must exist in the repository.</param>
    public void Update(Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        GetAll();

        // Find the index of the existing record. 
        // We match by email (case-insensitive)
        int index = _cache.FindIndex(e =>
            string.Equals(e.Mail, employee.Mail, StringComparison.OrdinalIgnoreCase));

        if (index != -1)
        {
            // Replace the old object with the updated one
            _cache[index] = employee;

            _logger.LogDebug("Updated employee {Email} in memory cache.", employee.Mail);

            // Persist the updated list to the CSV file
            SaveChanges();
        }
        else
        {
            _logger.LogWarning("Attempted to update non-existent employee with email {Email}",
                employee.Mail);
        }
    }

    /// <summary>
    /// Writes the current cache of employees back to the CSV file, overwriting it.
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
            csv.Context.RegisterClassMap<EmployeeMap>();

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