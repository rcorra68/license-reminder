namespace AvvisoScadenzaPatenti.Core.Interfaces;

using System.Threading.Tasks;

using AvvisoScadenzaPatenti.Core.Entities;

// Project: Core.Interfaces
public interface IEmailService
{
    void SendExpirationNotice(Employee employee, License license, bool isExpired);
    bool VerifyEmailConnectivity();
}