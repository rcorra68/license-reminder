namespace AvvisoScadenzaPatenti.Core.Interfaces;

using AvvisoScadenzaPatenti.Core.Models;

/// <summary>
/// Repository interface for managing uncompliant mail records in any persistence layer.
/// These records are used to override or complete employee email information.
/// </summary>
public interface IUncompliantMailRepository
{
    /// <summary>
    /// Gets all uncompliant mail records from the underlying storage (e.g. file, database).
    /// </summary>
    /// <returns>A list of all uncompliant mail records.</returns>
    Task<IEnumerable<UncompliantMail>> GetAllAsync();

    /// <summary>
    /// Finds an employee's email based on the first and last name in the uncompliant mail records.
    /// Returns a minimal employee object containing only the email, or null if no match is found.
    /// </summary>
    /// <param name="lastName">The last name to search for.</param>
    /// <param name="firstName">The first name to search for.</param>
    /// <returns>An Employee with the email from the uncompliant record, or null if not found.</returns>
    Task<Employee?> GetByNameAsync(string lastName, string firstName);
}