namespace AvvisoScadenzaPatenti.Core.Services;

using Microsoft.Extensions.Logging;

using AvvisoScadenzaPatenti.Core.Entities;
using AvvisoScadenzaPatenti.Core.Interfaces;
using AvvisoScadenzaPatenti.Core.Models;
using AvvisoScadenzaPatenti.Core.Shared;

/// <summary>
/// Coordinates the full license processing workflow.
/// Responsibilities include:
/// - Loading licenses from repository
/// - Resolving or creating employees
/// - Evaluating expiration rules
/// - Sending notifications for expired or expiring licenses
/// </summary>
public class LicenseOrchestrator
{
    private readonly ILicenseRepository _licenseRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IUncompliantMailRepository _uncompliantRepo;
    private readonly IEmailService _emailService;
    private readonly ILogger<LicenseOrchestrator> _logger;

    /// <summary>
    /// Represents a warning threshold configuration for license expiration notifications.
    /// Each threshold defines:
    /// - number of days
    /// - condition flag getter
    /// - condition flag setter
    /// - human-readable label
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
    /// Executes the complete license processing pipeline.
    /// This includes loading data, evaluating expiration rules, and triggering notifications.
    /// </summary>
    public async Task ProcessLicensesAsync(CancellationToken ct = default)
    {
        var start = DateTime.UtcNow;

        int processed = 0;
        int sent = 0;
        int errors = 0;

        try
        {
            _logger.LogInformation(
                "License processing started. Version {Version} | Environment {Environment}",
                AppVersion.Get(),
                Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"));

            var licenses = _licenseRepo.GetAll();

            foreach (var license in licenses)
            {
                ct.ThrowIfCancellationRequested();

                processed++;

                var employee = GetOrCreateEmployee(license.FirstName, license.LastName);

                try
                {
                    bool emailSent = await EvaluateExpiryAsync(license, employee, ct);
                    if (emailSent)
                        sent++;
                }
                catch (Exception ex)
                {
                    errors++;

                    _logger.LogError(ex,
                        "Error processing license for {Email}",
                        employee.Mail);
                }
            }
        }
        catch (Exception)
        {
            errors++;
            throw;
        }
        finally
        {
            var report = new DailyReport
            {
                ExecutionDate = DateTime.UtcNow,
                ProcessedEmployees = processed,
                EmailsSent = sent,
                Errors = errors,
                ExecutionTime = DateTime.UtcNow - start
            };

            await _emailService.SendDailySummaryReportAsync(report, ct);
        }

        _logger.LogInformation("License processing completed successfully.");
    }

    /// <summary>
    /// Retrieves an existing employee or creates a new one if not found.
    /// Email is resolved using either:
    /// - compliant naming convention
    /// - or fallback from uncompliant repository
    /// </summary>
    private Employee GetOrCreateEmployee(string firstName, string lastName)
    {
        var employee = _employeeRepo.GetByName(firstName, lastName);

        if (employee != null)
            return employee;

        _logger.LogInformation("Creating new employee record for {FirstName} {LastName}", firstName, lastName);

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
    /// Evaluates the expiration state of a license and triggers appropriate actions.
    /// </summary>
    private Task<bool> EvaluateExpiryAsync(License license, Employee employee, CancellationToken ct)
    {
        int daysToExpiration =
            (license.ExpiryDate.Date - DateTime.UtcNow.Date).Days;

        if (daysToExpiration < 0)
            return HandleExpiredAsync(license, employee, daysToExpiration, ct);

        return HandleUpcomingExpirationAsync(license, employee, daysToExpiration, ct);
    }

    /// <summary>
    /// Handles expired licenses.
    /// Sends:
    /// - daily reminders for the first 3 days after expiration
    /// - periodic reminders every 14 days afterwards
    /// </summary>
    private async Task<bool> HandleExpiredAsync(License license, Employee employee, int days, CancellationToken ct)
    {
        int daysSinceExpiration = Math.Abs(days);

        if (daysSinceExpiration is >= 1 and <= 3)
        {
            await SendExpiredMailAsync(employee, license, "Daily expired notice", ct);
            return true;
        }

        if (daysSinceExpiration > 3 && (daysSinceExpiration - 3) % 14 == 0)
        {
            await SendExpiredMailAsync(employee, license, "Periodic expired notice", ct);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles upcoming license expirations using predefined warning thresholds.
    /// Ensures notifications are sent only once per threshold.
    /// </summary>
    private async Task<bool> HandleUpcomingExpirationAsync(License license, Employee employee, int days, CancellationToken ct)
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
            return false;
        }

        if (!active.GetFlag(employee))
        {
            _logger.LogInformation(
                "Sending {Label} warning to {Email} ({Days} days left)",
                active.Label, employee.Mail, days);

            active.SetFlag(employee, true);

            await _emailService.SendExpirationNoticeAsync(
                employee,
                license,
                isExpired: false,
                ct);

            _employeeRepo.Update(employee);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Resets all warning flags when no active thresholds apply.
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
    /// Resolves the email address for a new employee.
    /// Uses uncompliant repository fallback if available.
    /// </summary>
    private string ResolveEmail(string firstName, string lastName, UncompliantMail? uncompliant)
    {
        return uncompliant?.Mail
            ?? $"{firstName.ToLower().Trim()}.{lastName.ToLower().Trim()}@vigilfuoco.it";
    }

    /// <summary>
    /// Sends an email notification for expired licenses.
    /// </summary>
    private async Task SendExpiredMailAsync(Employee employee, License license, string reason, CancellationToken ct)
    {
        await _emailService.SendExpirationNoticeAsync(employee, license, isExpired: true, ct);

        _logger.LogInformation("{Reason} sent to {Email}", reason, employee.Mail);
    }
}