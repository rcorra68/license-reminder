namespace avviso_scadenza_patenti.Controllers
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using avviso_scadenza_patenti.Entities;

    using CsvHelper;

    using Serilog;

    internal class EmployeeController
    {
        protected static IList<Employee> employees;
        private static readonly string csvFilename = @".\Employees.csv";

        static EmployeeController()
        {
            // Check if file exists
            if (!File.Exists(csvFilename))
            {
                Log.Debug("File does not exists! Created.");
                using (var sw = new StreamWriter(csvFilename, true))
                {
                    sw.WriteLine("COGNOME,NOME,POSTA_ELETTRONICA,DUE_MESI,UN_MESE,DUE_SETTIMANE,UNA_SETTIMANA,UN_GIORNO");
                }
            }

            using (var reader = new StreamReader(csvFilename))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<EmployeeMap>();
                employees = csv.GetRecords<Employee>().ToList();
                Log.Debug("Exmployees found: {0}.", employees.Count);
            }
        }

        public static IList<Employee> List()
        {
            return employees.ToList();
        }
        public static Employee Get(string lastName, string firstName)
        {
            return List().FirstOrDefault(e => e.LastName == lastName && e.FirstName == firstName);
        }

        public static Employee Create(string lastName, string firstName) {
            Employee employee = new Employee
            {
                LastName = lastName,
                FirstName = firstName,
                Mail = Regex.Replace($"{firstName}.{lastName}@vigilfuoco.it".ToLower(), @"\s+", "")
            };

            // Controlla l'esistenza di una mail non conforme allo standard nome.cognome@vigilfuoco.it
            UncompliantMail uncompliantMail = UncompliantMailController.Get(lastName, firstName);
            if (uncompliantMail != null)
            {
                employee.Mail = uncompliantMail.Mail;
            }

            employees.Add(employee);
            Save();

            return employee;
        }
        public static void Save() {
            using (var writer = new StreamWriter(csvFilename))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<EmployeeMap>();
                csv.WriteRecords(employees.OrderBy(e => e.LastName).ThenBy(e => e.FirstName));
            }
        }
    }
}
