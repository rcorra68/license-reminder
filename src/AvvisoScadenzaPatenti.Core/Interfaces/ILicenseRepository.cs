namespace AvvisoScadenzaPatenti.Core.Interfaces;

using AvvisoScadenzaPatenti.Core.Entities;

/// <summary>
/// Repository interface for managing license entities in any persistence layer.
/// </summary>
public interface ILicenseRepository
{
    /// <summary>
    /// Gets all licenses from the underlying storage (e.g. file, database).
    /// </summary>
    /// <returns>A list of all licenses.</returns>
    IEnumerable<License> GetAll();

    License? GetByLicenseNumber(string licenseNumber);
}