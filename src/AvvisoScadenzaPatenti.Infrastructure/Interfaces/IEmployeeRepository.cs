namespace AvvisoScadenzaPatenti.Core.Interfaces;

using AvvisoScadenzaPatenti.Core.Models;

public interface IEmployeeRepository
{
    IEnumerable<Employee> GetAll();
    Employee? GetByEmail(string email);
    void Add(Employee employee);
    void SaveChanges(); // Essenziale per i file flat: scrive tutto su disco
}