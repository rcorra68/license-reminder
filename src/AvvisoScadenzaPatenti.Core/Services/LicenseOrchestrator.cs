namespace AvvisoScadenzaPatenti.Core.Services;

using System.Net.Mail;

using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Models;

using Microsoft.Extensions.Logging;

using MimeKit;

public class LicenseOrchestrator
{
    private readonly ILicenseRepository _licenseRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IUncompliantMailRepository _uncompliantRepo;
    private readonly IEmailService _emailService;
    private readonly ILogger<LicenseOrchestrator> _logger;

    // Define a simple structure for thresholds
    private record struct WarningThreshold(int Days, Func<Employee, bool> GetFlag, Action<Employee, bool> SetFlag, string Label);

    public LicenseOrchestrator(
        ILicenseRepository licenseRepo,
        IEmployeeRepository employeeRepo,
        IUncompliantMailRepository uncompliantRepo,
        IEmailService emailService,
        ILogger<LicenseOrchestrator> logger)
    {
        _licenseRepo = licenseRepo;
        _employeeRepo = employeeRepo;
        _uncompliantRepo = uncompliantRepo;
        _emailService = emailService;
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
            var employee = await this.GetOrCreateEmployeeAsync(license.FirstName, license.LastName);

            // Evaluate and handle expiration logic for this license and employee
            await this.EvaluateExpiryAsync(license, employee);
        }

        // Log successful completion of the processing
        _logger.LogInformation("Processing completed successfully.");
    }

    /// <summary>
    /// Retrieves an existing employee or creates a new one with a calculated or uncompliant email.
    /// </summary>
    private async Task<Employee> GetOrCreateEmployeeAsync(string firstName, string lastName)
    {
        // Check if the employee already exists (case-insensitive check)
        var employee = await _employeeRepo.GetByNameAsync(firstName, lastName);

        if (employee != null)
        {
            return employee;
        }

        _logger.LogInformation("Creating new record for {FirstName} {LastName}", firstName, lastName);

        // Check for uncompliant mail
        var uncompliant = await _uncompliantRepo.GetByNameAsync(firstName, lastName);
        string email = uncompliant?.Mail ?? $"{firstName.ToLower().Trim()}.{lastName.ToLower().Trim()}@vigilfuoco.it";

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
        try
        {
            // Usiamo UtcNow per coerenza DevOps/Cloud
            int daysToExpiration = (license.ExpiryDate.Date - DateTime.UtcNow.Date).Days;

            if (daysToExpiration < 0)
            {
                await HandleExpiredAsync(license, employee, daysToExpiration);
            }
            else
            {
                await HandleUpcomingExpirationAsync(license, employee, daysToExpiration);
            }
        }
        catch (Exception ex)
        {
            // Qui usiamo il _logger dell'istanza! Se fosse statico non potremmo.
            _logger.LogError(ex, "Error evaluating expiry for {LastName}", employee.LastName);
        }
    }

    private async Task HandleExpiredAsync(License license, Employee employee, int days)
    {
        // Logic for already expired licenses (e.g., notification every 14 days)
        if (days % 14 == -1 || days > -3)
        {
            await _emailService.SendExpirationNoticeAsync(employee, license, isExpired: true);
            _logger.LogInformation("Sent 'Expired' notification to {Email}", employee.Mail);
        }
    }
    private async Task HandleUpcomingExpirationAsync(License license, Employee employee, int days)
    {
        // 1. Define thresholds in descending order
        var thresholds = new[]
        {
            new WarningThreshold(60, e => e.Warning2Months, (e, v) => e.Warning2Months = v, "Two Months"),
            new WarningThreshold(30, e => e.Warning1Month,  (e, v) => e.Warning1Month = v,  "One Month"),
            new WarningThreshold(14, e => e.Warning2Weeks,  (e, v) => e.Warning2Weeks = v,  "Two Weeks"),
            new WarningThreshold(7,  e => e.Warning1Week,   (e, v) => e.Warning1Week = v,   "One Week"),
            new WarningThreshold(1,  e => e.Warning1Day,    (e, v) => e.Warning1Day = v,    "One Day")
        };

        // 2. Find the most stringent threshold that applies to remaining days
        // Example: if 10 days remain, it will select the 14-day threshold (because 10 <= 14)
        var activeThreshold = thresholds.FirstOrDefault(t => days <= t.Days);

        // If no thresholds are active (e.g., 90 days remain)
        if (activeThreshold == default)
        {
            if (employee.HasAnyWarningActive()) // Helper method for cleaner code
            {
                employee.ResetAllWarnings();
                await _employeeRepo.UpdateAsync(employee);
                _logger.LogDebug("Reset all warning flags for {Email}", employee.Mail);
            }
            return;
        }

        // 3. Check if notification for this threshold has already been sent
        if (!activeThreshold.GetFlag(employee))
        {
            _logger.LogInformation("Processing {Label} expiration for {Email} ({Days} days left)",
                activeThreshold.Label, employee.Mail, days);

            // Set the flag, send the email, and save
            activeThreshold.SetFlag(employee, true);

            await _emailService.SendExpirationNoticeAsync(employee, license, isExpired: false);
            await _employeeRepo.UpdateAsync(employee);

            _logger.LogInformation("Successfully notified {Email} for {Label} threshold",
                employee.Mail, activeThreshold.Label);
        }
    }
}