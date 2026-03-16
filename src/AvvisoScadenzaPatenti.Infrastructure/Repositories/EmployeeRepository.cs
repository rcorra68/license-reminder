namespace AvvisoScadenzaPatenti.Infrastructure.Repositories;

using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Models;
using AvvisoScadenzaPatenti.Core.Mappings;

/// <summary>
/// Implementation of IEmployeeRepository using CsvHelper for flat-file storage.
/// </summary>
public class EmployeeRepository : IEmployeeRepository
{
    private readonly string _filePath;
    private readonly CsvConfiguration _csvConfig;
    private List<Employee> _employees = new();

    public CsvEmployeeRepository(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        
        // Initialize CsvHelper configuration for invariant culture
        _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null // Ignore missing fields to avoid runtime exceptions
        };

        LoadData();
    }

    /// <summary>
    /// Loads all records from the CSV file into the memory cache.
    /// </summary>
    private void LoadData()
    {
        if (!File.Exists(_filePath))
        {
            Log.Debug("File does not exists! Created.");
            using (var sw = new StreamWriter(filePath, true))
            {
                sw.WriteLine("COGNOME,NOME,POSTA_ELETTRONICA,DUE_MESI,UN_MESE,DUE_SETTIMANE,UNA_SETTIMANA,UN_GIORNO");
            }

            _employees = new List<Employee>();
            return;
        }

        using var reader = new StreamReader(_filePath);
        using var csv = new CsvReader(reader, _csvConfig);
        
        // Register the Fluent Mapping class
        csv.Context.RegisterClassMap<EmployeeMap>();
        
        _employees = csv.GetRecords<Employee>().ToList();
    }

    /// <summary>
    /// Returns all employees currently in the cache.
    /// </summary>
    public IEnumerable<Employee> GetAll() => _employees;

    /// <summary>
    /// Finds a specific employee by their email address (case-insensitive).
    /// </summary>
    public Employee? GetByEmail(string email) 
    {
        return _employees.FirstOrDefault(e => 
            e.Mail.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a specific employee by their lastName and firstName (case-insensitive).
    /// </summary>
    public static Employee GetByName(string lastName, string firstName)
    {
        return _employees.FirstOrDefault(e => 
            e.LastName == lastName &&
            e.FirstName == firstName);
    }

    /// <summary>
    /// Adds a new employee instance to the local cache.
    /// </summary>
    public void Add(Employee employee) 
    {
        if (employee == null) throw new ArgumentNullException(nameof(employee));
        _employees.Add(employee);
    }

    /// <summary>
    /// Persists the current state of the employee list back to the CSV file.
    /// </summary>
    public void SaveChanges()
    {
        using var writer = new StreamWriter(_filePath);
        using var csv = new CsvWriter(writer, _csvConfig);
        
        csv.Context.RegisterClassMap<EmployeeMap>();
        csv.WriteRecords(_employees);
    }
}