namespace AvvisoScadenzaPatenti.Core.Interfaces;

using System.Threading.Tasks;

using AvvisoScadenzaPatenti.Core.Models;

// Project: Core.Interfaces
public interface IEmailService
{
    Task SendExpirationNoticeAsync(Employee employee, License license, bool isExpired);
}