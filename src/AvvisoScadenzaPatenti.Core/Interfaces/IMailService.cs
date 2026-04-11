namespace AvvisoScadenzaPatenti.Core.Interfaces;

using AvvisoScadenzaPatenti.Core.Entities;
using AvvisoScadenzaPatenti.Core.Models;

// Project: Core.Interfaces
public interface IEmailService
{
    void SendExpirationNotice(Employee employee, License license, bool isExpired);
    bool VerifyEmailConnectivity();
    void SendDailySummaryReport(DailyReport report);
}