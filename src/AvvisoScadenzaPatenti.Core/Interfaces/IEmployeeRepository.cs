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
    Task<IEnumerable<Employee>> GetAllAsync();

    /// <summary>
    /// Finds an employee by email address, ignoring case.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <returns>The matching employee, or null if not found.</returns>
    Task<Employee?> GetByEmailAsync(string email);

    /// <summary>
    /// Finds an employee by first and last name.
    /// </summary>
    /// <param name="lastName">The last name to search for.</param>
    /// <param name="firstName">The first name to search for.</param>
    /// <returns>The matching employee, or null if not found.</returns>
    Task<Employee?> GetByNameAsync(string lastName, string firstName);

    /// <summary>
    /// Adds a new employee to the repository.
    /// After this call, the repository is responsible for persisting the change.
    /// </summary>
    /// <param name="employee">The employee to add. Must not be null.</param>
    Task AddAsync(Employee employee);
}