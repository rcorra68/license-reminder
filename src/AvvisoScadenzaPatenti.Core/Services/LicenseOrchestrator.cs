namespace AvvisoScadenzaPatenti.Core;

using AvvisoScadenzaPatenti.Core.Models;
using AvvisoScadenzaPatenti.Infrastructure.Repositories;
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

    public void ProcessLicenses()
    {
        var licenses = _licenseRepo.GetAll();

        foreach (var license in licenses)
        {
            // 1. Try to find the employee
            var employee = _employeeRepo.GetByName(license.FirstName, license.LastName);

            if (employee == null)
            {
                _logger.LogInformation("Employee {First} {Last} not found. Handling new record...", license.FirstName, license.LastName);
                employee = CreateNewEmployee(license.FirstName, license.LastName);
            }

            // 2. Process Expiry Logic (to be detailed later)
            EvaluateExpiry(license, employee);
        }

        // 3. Persist changes to Employee CSV if any new records were added
        _employeeRepo.SaveChanges();
    }

    private Employee CreateNewEmployee(string firstName, string lastName)
    {
        // Check for uncompliant emails first
        var uncompliant = _uncompliantRepo.GetByFullname(firstName, lastName);
        
        string email = uncompliant != null 
            ? uncompliant.Email 
            : $"{firstName.ToLower()}.{lastName.ToLower()}@company.com";

        var newEmployee = new Employee
        {
            FirstName = firstName,
            LastName = lastName,
            Mail = email,
            Warning2Months = false, // Default values
            Warning1Month = false, // Default values
            Warning2Weeks = false, // Default values
            Warning1Week = false, // Default values
            Warning1Day = false, // Default values
        };

        _employeeRepo.Add(newEmployee);
        return newEmployee;
    }

    private void EvaluateExpiry(License license, Employee employee)
    {
        // Placeholder for your contorted logic
        // TODO: Implement date comparison and email trigger logic
    }
}