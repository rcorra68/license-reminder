namespace AvvisoScadenzaPatenti.Core.Interfaces;

using System.ComponentModel;

using AvvisoScadenzaPatenti.Core.Models;

public interface IEmployeeRepository
{
    IEnumerable<Employee> GetAll();
    Employee? GetByEmail(string email);
    Employee? GetByName(string firstName, string lastName);
    void Add(Employee employee);
    void SaveChanges(); // Essenziale per i file flat: scrive tutto su disco
}