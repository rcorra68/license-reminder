namespace AvvisoScadenzaPatenti.Core.Services;

using AvvisoScadenzaPatenti.Core.Entities;
using AvvisoScadenzaPatenti.Core.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates the license processing workflow:
/// - Loads licenses
/// - Ensures employee existence
/// - Evaluates expiration rules
/// - Sends notifications
/// </summary>
public class LicenseOrchestrator
{
    private readonly ILicenseRepository _licenseRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IUncompliantMailRepository _uncompliantRepo;
    private readonly IEmailService _emailService;
    private readonly ILogger<LicenseOrchestrator> _logger;

    /// <summary>
    /// Represents a warning threshold rule for upcoming expirations.
    /// </summary>
    private readonly record struct WarningThreshold(
        int Days,
        Func<Employee, bool> GetFlag,
        Action<Employee, bool> SetFlag,
        string Label);

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
    /// Executes the full license processing pipeline.
    /// </summary>
    public void ProcessLicenses()
    {
        _logger.LogInformation(
            "AvvisoScadenzaPatenti starting version {Version} ({Environment})", 
            AppVersion.Get(), 
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"));

        var licenses = _licenseRepo.GetAll();

        foreach (var license in licenses)
        {
            var employee = GetOrCreateEmployee(license.FirstName, license.LastName);
            EvaluateExpiry(license, employee);
        }

        _logger.LogInformation("License processing completed successfully.");
    }

    /// <summary>
    /// Retrieves an existing employee or creates a new one using either:
    /// - compliant email generation
    /// - or fallback from uncompliant list
    /// </summary>
    private Employee GetOrCreateEmployee(string firstName, string lastName)
    {
        var employee = _employeeRepo.GetByName(firstName, lastName);

        if (employee != null)
            return employee;

        _logger.LogInformation("Creating employee record for {FirstName} {LastName}", firstName, lastName);

        var uncompliant = _uncompliantRepo.GetByName(firstName, lastName);
        var email = ResolveEmail(firstName, lastName, uncompliant);

        var newEmployee = new Employee
        {
            FirstName = firstName,
            LastName = lastName,
            Mail = email
        };

        _employeeRepo.Add(newEmployee);

        return newEmployee;
    }

    /// <summary>
    /// Evaluates expiration state of a license and triggers notifications.
    /// </summary>
    private void EvaluateExpiry(License license, Employee employee)
    {
        try
        {
            int daysToExpiration =
                (license.ExpiryDate.Date - DateTime.UtcNow.Date).Days;

            if (daysToExpiration < 0)
                HandleExpired(license, employee, daysToExpiration);
            else
                HandleUpcomingExpiration(license, employee, daysToExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error evaluating expiry for employee {Email}",
                employee.Mail);
        }
    }

    /// <summary>
    /// Handles licenses that are already expired.
    /// Sends:
    /// - daily notifications for first 3 days
    /// - then every 14 days thereafter
    /// </summary>
    private void HandleExpired(License license, Employee employee, int days)
    {
        int daysSinceExpiration = Math.Abs(days);

        // First 3 days after expiration: daily reminders
        if (daysSinceExpiration is >= 1 and <= 3)
        {
            SendExpiredMail(employee, license, "Daily expired notice");
            return;
        }

        // Every 14 days after the first 3 days
        if (daysSinceExpiration > 3 && (daysSinceExpiration - 3) % 14 == 0)
        {
            SendExpiredMail(employee, license, "Periodic expired notice");
        }
    }

    /// <summary>
    /// Handles upcoming expirations using predefined warning thresholds.
    /// </summary>
    private void HandleUpcomingExpiration(License license, Employee employee, int days)
    {
        var thresholds = new[]
        {
            new WarningThreshold(60, e => e.Warning2Months, (e,v) => e.Warning2Months = v, "Two Months"),
            new WarningThreshold(30, e => e.Warning1Month,  (e,v) => e.Warning1Month = v,  "One Month"),
            new WarningThreshold(14, e => e.Warning2Weeks,  (e,v) => e.Warning2Weeks = v,  "Two Weeks"),
            new WarningThreshold(7,  e => e.Warning1Week,   (e,v) => e.Warning1Week = v,   "One Week"),
            new WarningThreshold(1,  e => e.Warning1Day,    (e,v) => e.Warning1Day = v,    "One Day")
        };

        var active = thresholds.FirstOrDefault(t => days <= t.Days);

        if (active == default)
        {
            ResetWarningsIfNeeded(employee);
            return;
        }

        if (!active.GetFlag(employee))
        {
            _logger.LogInformation(
                "Sending {Label} warning to {Email} ({Days} days left)",
                active.Label, employee.Mail, days);

            active.SetFlag(employee, true);

            _emailService.SendExpirationNotice(employee, license, isExpired: false);
            _employeeRepo.Update(employee);
        }
    }

    /// <summary>
    /// Resets all warning flags if needed.
    /// </summary>
    private void ResetWarningsIfNeeded(Employee employee)
    {
        if (!employee.HasAnyWarningActive())
            return;

        employee.ResetAllWarnings();
        _employeeRepo.Update(employee);

        _logger.LogDebug("Reset warning flags for {Email}", employee.Mail);
    }

    /// <summary>
    /// Builds an email address for a new employee.
    /// </summary>
    private string ResolveEmail(string firstName, string lastName, UncompliantMail? uncompliant)
    {
        return uncompliant?.Mail
            ?? $"{firstName.ToLower().Trim()}.{lastName.ToLower().Trim()}@vigilfuoco.it";
    }

    /// <summary>
    /// Sends an expired license notification email.
    /// </summary>
    private void SendExpiredMail(Employee employee, License license, string reason)
    {
        _emailService.SendExpirationNotice(employee, license, isExpired: true);

        _logger.LogInformation("{Reason} sent to {Email}", reason, employee.Mail);
    }
}