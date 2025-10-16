namespace avviso_scadenza_patenti
{
    // See https://aka.ms/new-console-template for more information

    using System;
    using System.Text;

    using avviso_scadenza_patenti.AppSettings;
    using avviso_scadenza_patenti.Controllers;
    using avviso_scadenza_patenti.Entities;

    using MailKit.Net.Smtp;
    using MailKit.Security;

    using Microsoft.Extensions.Configuration;

    using MimeKit;

    using NDesk.Options;

    using Serilog;

    class Program
    {
        private static Settings settings = new Settings();

        static void Main(string[] args)
        {
            try
            {
                bool showHelp = false;
                bool cryptPassword = false;
                string password = string.Empty;

                var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile($"{AppDomain.CurrentDomain.BaseDirectory}appsettings.json")
                        .Build();

                settings = configuration.GetSection("Settings").Get<Settings>();
                if (settings == null)
                {
                    throw new InvalidOperationException("AppSettings.json not loaded!");
                }

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .CreateLogger();
                Log.Information("Starting up!");

                // Controllo dei parametri a linea di comando
                var p = new OptionSet()
                {
                    { "c|crypt=", v => {
                        cryptPassword = true;
                        password = v;
                    } },
                    { "h|?|help", v => { showHelp = v != null; } }
                };

                
                p.Parse(args);

                if (cryptPassword)
                {
                    byte[] binaryData = Encoding.UTF8.GetBytes(password);
                    string encodedPassword = Convert.ToBase64String(binaryData);

                    settings.MailServer.Password = encodedPassword;
                    Log.Debug($"Write new encoded password: {encodedPassword}");
                }
                else
                {
                    // Esegue il controllo sulla scadenza delle patenti
                    DriveLicenseIterate();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
            }
        }

        private static void DriveLicenseIterate()
        {
            // Esegue un loop sulla
            foreach (License license in LicenseController.List())
            {
                // Per ogni patente controlla se la relativa anagrafica è presente in archivio, altrimenti la crea
                Employee employee = EmployeeController.Get(license.LastName, license.FirstName);
                if (employee == null)
                {
                    employee = EmployeeController.Create(license.LastName, license.FirstName);
                    Log.Information($"Create new Employee: {employee.LastName} {employee.FirstName} - {employee.Mail}");
                }

                // Verifica la scadenza della patente
                CheckExpiration(license, employee);
            }
        }

        private static void CheckExpiration(License license, Employee employee)
        {
            try
            {
                TimeSpan expirationTime = license.ExpirationDate - DateTime.Now;

                if (expirationTime.Days < 0)
                {
                    // Patente scaduta
                    Log.Debug($"Licenze expired for {employee.LastName} {employee.FirstName}: {expirationTime.Days}.");

                    if (expirationTime.Days % 14 == -1 || expirationTime.Days > -3)
                    {
                        SendMail(employee, license);
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
                                SendMail(employee, license);
                                EmployeeController.Save();
                                Log.Information($"Send mail to {employee.Mail} for expiration in 'One days'. Expiration Date: {license.ExpirationDate:dd-MM-yyyy}");
                            }
                            break;
                        case int n when (n > 1 && n <= 7):
                            if (!employee.Warning1Week)
                            {
                                employee.Warning1Week = true;
                                SendMail(employee, license);
                                EmployeeController.Save();
                                Log.Information($"Send mail to {employee.Mail} for expiration in 'One Week'. Expiration Date: {license.ExpirationDate:dd-MM-yyyy}");
                            }
                            break;
                        case int n when (n > 7 && n <= 14):
                            if (!employee.Warning2Weeks)
                            {
                                employee.Warning2Weeks = true;
                                SendMail(employee, license);
                                EmployeeController.Save();
                                Log.Information($"Send mail to {employee.Mail} for expiration in 'Two Weeks'. Expiration Date: {license.ExpirationDate:dd-MM-yyyy}");
                            }
                            break;
                        case int n when (n > 14 && n <= 30):
                            if (!employee.Warning1Month)
                            {
                                employee.Warning1Month = true;
                                SendMail(employee, license);
                                EmployeeController.Save();
                                Log.Information($"Send mail to {employee.Mail} for expiration in 'One Month'. Expiration Date: {license.ExpirationDate:dd-MM-yyyy}");
                            }
                            break;
                        case int n when (n > 30 && n <= 60):
                            if (!employee.Warning2Months)
                            {
                                employee.Warning2Months = true;
                                SendMail(employee, license);
                                EmployeeController.Save();
                                Log.Information($"Send mail to {employee.Mail} for expiration in 'Two Months'. Expiration Date: {license.ExpirationDate:dd-MM-yyyy}");
                            }
                            break;
                        default:
                            employee.Warning1Day = false;
                            employee.Warning1Week = false;
                            employee.Warning2Weeks = false;
                            employee.Warning1Month = false;
                            employee.Warning2Months = false;
                            EmployeeController.Save();
                            Log.Debug($"Reset all warning flags for {employee.Mail}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
            }
        }
        private static void SendMail(Employee employee, License license)
        {
            try
            {
                using (var client = new SmtpClient())
                {
                    // Connect to SMTP server with SSL
                    client.Connect(settings.MailServer.Host, 465, SecureSocketOptions.SslOnConnect);

                    // Authenticate with your SMTP credentials
                    byte[] binaryData = Convert.FromBase64String(settings.MailServer.Password);
                    string decodedPassword = Encoding.UTF8.GetString(binaryData);
                    client.Authenticate(settings.MailServer.Username, decodedPassword);

                    // Create warning message
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress("Procedura Patenti WEB", "noreply@vigilfuoco.it"));
#if DEBUG
                    message.To.Add(new MailboxAddress($"{employee.FirstName} {employee.LastName}", "roberto.corradetti@vigilfuoco.it"));
                    message.Subject = "*** DEBUG *** Avviso scadenza patente *** DEBUG ***";
#else
                    message.To.Add(new MailboxAddress($"{employee.FirstName} {employee.LastName}", employee.Mail));
                    foreach (string mailBcc in settings.MailBcc)
                    {
                        message.Bcc.Add(new MailboxAddress(mailBcc, mailBcc));
                    }

                    message.Subject = "Avviso scadenza patente";
#endif
                    message.Body = MailHtmlBody(employee, license);

                    client.Send(message);
                    client.Disconnect(true);
                }

                Log.Information($"Successfully send message to: {employee.Mail}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error when send message to: {employee.Mail} ({ex.ToString()})");
            }
        }

        private static MimeEntity MailHtmlBody(Employee employee, License license)
        {
            var builder = new BodyBuilder();

            try
            {
                var cultureInfoIta = System.Globalization.CultureInfo.GetCultureInfo("it-IT");

                builder.HtmlBody = $"Buongiorno {employee.FirstName} {employee.LastName}<br/>";
                builder.HtmlBody += $"<br/>";
                if (DateTime.Now > license.ExpirationDate)
                {
                    builder.HtmlBody += $"La tua patente è <b>SCADUTA</b><br/>";
                    Log.Debug(string.Create(cultureInfoIta, $"License expired: {license.ExpirationDate:d}"));
                }
                else
                {
                    builder.HtmlBody += string.Create(cultureInfoIta, $"La tua patente di <b>{license.Category}</b> scadrà il/l' <b>{license.ExpirationDate:d}</b>.<br/>");
                    Log.Debug(string.Create(cultureInfoIta, $"License expiring on {license.ExpirationDate:d}"));
                }
                builder.HtmlBody += "<br/>";
                builder.HtmlBody += "Se hai già provveduto al rinnovo, ignora la presente mail. Altrimenti chiedi all'IIE ROBERTO CORRADETTI cosa fare per il rinnovo.";
                builder.HtmlBody += "<br/>";
                builder.HtmlBody += "<br/>";
                builder.HtmlBody += "<small>*** La presente mail è generata automaticamente dal sistema. Per qualsiasi comunicazione, si prega di non rispondere a questa mail, ma di contattare l'help desk tecnico ***.</small>";

            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
            }

            return builder.ToMessageBody();
        }
    }
}
