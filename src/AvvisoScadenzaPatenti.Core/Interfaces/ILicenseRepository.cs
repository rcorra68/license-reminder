namespace AvvisoScadenzaPatenti.Core.Interfaces;

using AvvisoScadenzaPatenti.Core.Entities;

public interface ILicenseRepository
{
    IEnumerable<License> GetAll();
    License? GetByLicenseNumber(string licenseNumber);
    IEnumerable<License> SearchByName(string query);
    void SaveAll(IEnumerable<License> licenses);
}