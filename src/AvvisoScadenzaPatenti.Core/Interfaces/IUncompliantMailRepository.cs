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
    IEnumerable<UncompliantMail> GetAll();

    /// <summary>
    /// Retrieves the email address from uncompliant mail records by matching first and last name.
    /// Returns a minimal <see cref="UncompliantMail"/> object containing only the email, or <c>null</c> if no match found.
    /// </summary>
    /// <param name="firstName">The first name to search for (case-insensitive).</param>
    /// <param name="lastName">The last name to search for (case-insensitive).</param>
    /// <returns>
    /// A <see cref="UncompliantMail"/> object with the matching email from uncompliant records, 
    /// or <c>null</c> if no matching record is found.
    /// </returns>
    UncompliantMail? GetByName(string firstName, string lastName);
}