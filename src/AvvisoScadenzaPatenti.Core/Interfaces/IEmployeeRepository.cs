namespace AvvisoScadenzaPatenti.Core.Interfaces;

using AvvisoScadenzaPatenti.Core.Models;

/// <summary>
/// Repository interface for managing employee entities in any persistence layer.
/// </summary>
public interface IEmployeeRepository
{
    /// <summary>
    /// Gets all employees from the underlying storage (e.g. file, database).
    /// </summary>
    /// <returns>A list of all employees.</returns>
    IEnumerable<Employee> GetAll();

    /// <summary>
    /// Finds an employee by email address, ignoring case.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <returns>The matching employee, or null if not found.</returns>
    Employee? GetByEmail(string email);

    /// <summary>
    /// Finds an employee by first and last name.
    /// </summary>
    /// <param name="firstName">The first name to search for.</param>
    /// <param name="lastName">The last name to search for.</param>
    /// <returns>The matching employee, or null if not found.</returns>
    Employee? GetByName(string firstName, string lastName);

    /// <summary>
    /// Adds a new employee to the repository.
    /// After this call, the repository is responsible for persisting the change.
    /// </summary>
    /// <param name="employee">The employee to add. Must not be null.</param>
    /// <returns>A task representing the hronous add operation.</returns>
    void Add(Employee employee);

    /// <summary>
    /// Updates an existing employee in the repository.
    /// After this call, the repository is responsible for persisting the change.
    /// </summary>
    /// <param name="employee">The employee to update. Must not be null and must exist in the repository.</param>
    /// <returns>A task representing the hronous update operation.</returns>
    void Update(Employee employee);
}