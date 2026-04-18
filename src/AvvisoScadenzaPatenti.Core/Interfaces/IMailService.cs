namespace AvvisoScadenzaPatenti.Core.Interfaces;

using AvvisoScadenzaPatenti.Core.Entities;
using AvvisoScadenzaPatenti.Core.Models;

/// <summary>
/// Defines the contract for sending emails related to license expiration notices,
/// daily summary reports, and SMTP connectivity verification.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an expiration notice email to an employee regarding their driving license status.
    /// </summary>
    /// <param name="employee">The employee receiving the notification.</param>
    /// <param name="license">The license being checked for expiration.</param>
    /// <param name="isExpired">True if the license is expired; false if it's expiring soon.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    Task SendExpirationNoticeAsync(Employee employee, License license, bool isExpired, CancellationToken ct = default);

    /// <summary>
    /// Verifies SMTP connectivity by sending a health check email.
    /// </summary>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>True if the SMTP connection and send operation succeed; otherwise, false.</returns>
    Task<bool> VerifyEmailConnectivityAsync(CancellationToken ct = default);

    /// <summary>
    /// Sends a daily summary report to the configured admin email address.
    /// </summary>
    /// <param name="report">The daily report containing processed employees, emails sent, errors, and execution time.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    Task SendDailySummaryReportAsync(DailyReport report, CancellationToken ct = default);
}