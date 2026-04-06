namespace AvvisoScadenzaPatenti.Core.Services;

using Microsoft.Extensions.Logging;

using AvvisoScadenzaPatenti.Core.Models;
using AvvisoScadenzaPatenti.Core.Interfaces;
public class LicenseOrchestrator
{
    private readonly ILicenseRepository _licenseRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IUncompliantMailRepository _uncompliantRepo;
    private readonly ILogger<LicenseOrchestrator> _logger;

    public LicenseOrchestrator(
        ILicenseRepository licenseRepo,
        IEmployeeRepository employeeRepo,
        IUncompliantMailRepository uncompliantRepo,
        ILogger<LicenseOrchestrator> logger)
    {
        _licenseRepo = licenseRepo;
        _employeeRepo = employeeRepo;
        _uncompliantRepo = uncompliantRepo;
        _logger = logger;
    }

    /// <summary>
    /// Processes all licenses asynchronously and updates or creates associated employees.
    /// </summary>
    public async Task ProcessLicensesAsync()
    {
        // Do not use .Result here; use await instead
        var licenses = await _licenseRepo.GetAllAsync();

        foreach (var license in licenses)
        {
            // The repository handles finding or creating (and saving) the employee
            var employee = await GetOrCreateEmployeeAsync(license.FirstName, license.LastName);

            // Evaluate and handle expiration logic for this license and employee
            await EvaluateExpiryAsync(license, employee);
        }

        // Log successful completion of the processing
        _logger.LogInformation("Processing completed successfully.");
    }

    /// <summary>
    /// Retrieves an existing employee or creates a new one with a calculated or uncompliant email.
    /// </summary>
    private async Task<Employee> GetOrCreateEmployeeAsync(string firstName, string lastName, UncompliantMail? uncompliant = null)
    {
        // Check if the employee already exists (case-insensitive check)
        var employee = await _employeeRepo.GetByNameAsync(firstName, lastName);

        if (employee != null)
        {
            return employee;
        }

        _logger.LogInformation("Creating new record for {FirstName} {LastName}", firstName, lastName);

        // Build fallback email if no uncompliant record is provided
        string email = uncompliant?.Mail ?? 
                    $"{firstName.ToLower().Trim()}.{lastName.ToLower().Trim()}@vigilfuoco.it";

        var newEmployee = new Employee
        {
            FirstName = firstName,
            LastName = lastName,
            Mail = email
            // Warning flags are handled by Employee class defaults
        };

        // Persist the new record
        await _employeeRepo.AddAsync(newEmployee);

        return newEmployee;
    }
    private async Task EvaluateExpiryAsync(License license, Employee employee)
    {
        // Placeholder for your contorted logic
        // TODO: Implement date comparison and email trigger logic
    }
}