namespace avviso_scadenza_patenti
{
    // See https://aka.ms/new-console-template for more information

    using System;

    using avviso_scadenza_patenti.Controllers;
    using avviso_scadenza_patenti.Entities;

    using Serilog;

    class Program
    {

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(@".\log.txt", outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .MinimumLevel.Debug()
                .CreateLogger();
            Log.Information("Starting up!");

            // Esegue un loop sulla
            foreach (License license in LicenseController.List()) {

                // Per ogni patente controlla se la relativa anagrafica è presente in archivio, altrimenti la crea
                Employee employee = EmployeeController.Get(license.LastName, license.FirstName);
                if (employee == null)
                {
                    employee = EmployeeController.Create(license.LastName, license.FirstName);
                    Log.Information("Create new Employee: {0} {1} - {2}", employee.LastName, employee.FirstName, employee.Mail);
                }

                // Verifica la scadenza della patente
                Console.WriteLine("{0} - {1} - {2}", license.Office, license.LicenseType, license.ExpirationDate);
            }
        }
    }
}
