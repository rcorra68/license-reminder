namespace AvvisoScadenzaPatenti.Core.Interfaces;

using AvvisoScadenzaPatenti.Core.Models;

public interface ILicenseRepository
{
    IEnumerable<License> GetAll();
}