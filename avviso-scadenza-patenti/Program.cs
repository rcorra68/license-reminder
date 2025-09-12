namespace avviso_scadenza_patenti
{
    // See https://aka.ms/new-console-template for more information

    using System;
    using System.Net;
    using System.Net.Mail;

    using avviso_scadenza_patenti.AppSettings;
    using avviso_scadenza_patenti.Controllers;
    using avviso_scadenza_patenti.Entities;

    using Microsoft.Extensions.Configuration;

    using Serilog;

    class Program
    {
        private static Settings settings = new Settings();

        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
                    .Build();

            settings = configuration.GetSection("Settings").Get<Settings>();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
            Log.Information("Starting up!");

            // Esegue un loop sulla
            foreach (License license in LicenseController.List())
            {
                // Per ogni patente controlla se la relativa anagrafica è presente in archivio, altrimenti la crea
                Employee employee = EmployeeController.Get(license.LastName, license.FirstName);
                if (employee == null)
                {
                    employee = EmployeeController.Create(license.LastName, license.FirstName);
                    Log.Information("Create new Employee: {0} {1} - {2}", employee.LastName, employee.FirstName, employee.Mail);
                }

                // Verifica la scadenza della patente
                CheckExpiration(license, employee);
            }
        }

        private static void CheckExpiration(License license, Employee employee)
        {
            TimeSpan expirationTime = license.ExpirationDate - DateTime.Now;

            if (expirationTime.Days < 0)
            {
                // Patente scaduta
                Log.Debug($"Licenze expired for {employee.LastName} {employee.FirstName}: {expirationTime.Days}.");

                if (expirationTime.Days % 7 == 1)
                {
                    WriteMail(employee, license);
                    Log.Information($"Send mail to {employee.Mail} for expired license. Expiration Date: {license.ExpirationDate:dd-MM-yyyy}");
                }
            }
            else
            {
                // Patente in scadenza
                Log.Debug($"Days to expiration for {employee.LastName} {employee.FirstName}: {expirationTime.Days}.");

                switch (expirationTime.Days)
                {
                    case int n when (n <= 1):
                        if (!employee.Warning1Day)
                        {
                            employee.Warning1Day = true;
                            WriteMail(employee, license);
                            EmployeeController.Save();
                            Log.Information($"Send mail to {employee.Mail} for expiration in 'One days'. Expiration Date: {license.ExpirationDate:dd-MM-yyyy}");
                        }
                        break;
                    case int n when (n > 1 && n <= 7):
                        if (!employee.Warning1Week)
                        {
                            employee.Warning1Week = true;
                            WriteMail(employee, license);
                            EmployeeController.Save();
                            Log.Information($"Send mail to {employee.Mail} for expiration in 'One Week'. Expiration Date: {license.ExpirationDate:dd-MM-yyyy}");
                        }
                        break;
                    case int n when (n > 7 && n <= 14):
                        if (!employee.Warning2Weeks)
                        {
                            employee.Warning2Weeks = true;
                            WriteMail(employee, license);
                            EmployeeController.Save();
                            Log.Information($"Send mail to {employee.Mail} for expiration in 'Two Weeks'. Expiration Date: {license.ExpirationDate:dd-MM-yyyy}");
                        }
                        break;
                    case int n when (n > 14 && n <= 30):
                        if (!employee.Warning1Month)
                        {
                            employee.Warning1Month = true;
                            WriteMail(employee, license);
                            EmployeeController.Save();
                            Log.Information($"Send mail to {employee.Mail} for expiration in 'One Month'. Expiration Date: {license.ExpirationDate:dd-MM-yyyy}");
                        }
                        break;
                    case int n when (n > 30 && n <= 60):
                        if (!employee.Warning2Months)
                        {
                            employee.Warning2Months = true;
                            WriteMail(employee, license);
                            EmployeeController.Save();
                            Log.Information($"Send mail to {employee.Mail} for expiration in 'Two Months'. Expiration Date: {license.ExpirationDate:dd-MM-yyyy}");
                        }
                        break;
                }
            }
        }
        private static void SendMail(Employee employee, DateTime expirationDate)
        {
            try
            {
                // add from, to mailaddresses
                MailAddress from = new MailAddress("noreply@vigilfuoco.it");
                MailAddress to = new MailAddress("roberto.corradetti@vigilfuoco.it");
                MailMessage licenseMail = new MailMessage(from, to);

                // set subject and encoding
                licenseMail.Subject = "Avviso scadenza Patente Ministeriale Terrestre";
                licenseMail.SubjectEncoding = System.Text.Encoding.UTF8;

                // set body-message and encoding
                licenseMail.Body = "<b>Test Mail</b><br>using <b>HTML</b>.";
                licenseMail.BodyEncoding = System.Text.Encoding.UTF8;

                // text or html
                licenseMail.IsBodyHtml = true;

                // Send email
                SmtpClient smtpClient = new SmtpClient("smtp.vigilfuoco.it", 465);
                smtpClient.EnableSsl = true;
                smtpClient.Timeout = 20000;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential("roberto.corradetti@vigilfuoco.it", "Corra+1070");
                smtpClient.Send(licenseMail);

                smtpClient.Dispose();
                Log.Information($"Successfully send message to: {employee.Mail}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error when send message to: {employee.Mail} ({ex.ToString()})");
            }
        }

        private static void WriteMail(Employee employee, License license)
        {
            string mailFilename = $"{settings.MailOutputFolder}\\{license.LicenseNumber}.cmd";
            using (var sw = new StreamWriter(mailFilename, false))
            {
                sw.Write(@"C:\Utilities\Cmail.exe");
                sw.Write($" -host:{settings.MailServer.Username}:{settings.MailServer.Password}@{settings.MailServer.Host} -secureport");
                sw.Write(" -authtypes:PLAIN");
                sw.Write(" -from:noreply@vigilfuoco.it:\"Procedura Patenti WEB\"");
                sw.Write($" -to:{employee.Mail}:\"{employee.LastName} {employee.FirstName}\"");
                foreach (string mailBcc in settings.MailBcc)
                {
                    sw.Write($" -bcc:{mailBcc}");
                }
                sw.Write($" \"-subject:Procedura Patenti WEB - Avviso scadenza patente\"");
                sw.Write($" -body-html:{license.LicenseNumber}.txt");
            }
            MailHtmlBody(employee, license);
        }

        private static void MailHtmlBody(Employee employee, License license)
        {
            string mailFilename = $"{settings.MailOutputFolder}\\{license.LicenseNumber}.txt";
            using (var sw = new StreamWriter(mailFilename, false))
            {
                sw.WriteLine($"Buongiorno {employee.FirstName} {employee.LastName}<br/>");
                sw.WriteLine($"<br/>");
                if (DateTime.Now > license.ExpirationDate)
                {
                    sw.WriteLine($"La tua patente è <b>SCADUTA</b><br/>");
                }
                else
                {
                    sw.WriteLine($"La tua patente di <b>{license.Category}</b> scadrà il/l' <b>{license.ExpirationDate:d}</b>.<br/>");
                }
                sw.WriteLine($"<br/>");
                sw.WriteLine($"Se hai già provvuduto al rinnovo, ignora la presente mail. Altrimenti chiedi all'IIE ROBERTO CORRADETTI cosa fare per il rinnovo.");
                sw.WriteLine($"<br/>");
                sw.WriteLine($"<br/>");
                sw.WriteLine($"<small>*** La presente mail è generata automaticamente dal sistema. Per qualsiasi comunicazione, si prega di non rispondere a questa mail, ma di contattare l'help desk tecnico ***.</small>");
            }
        }
    }
}
