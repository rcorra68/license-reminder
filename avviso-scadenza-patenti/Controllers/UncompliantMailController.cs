namespace avviso_scadenza_patenti.Controllers
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using avviso_scadenza_patenti.Entities;

    using CsvHelper;

    using Serilog;

    internal class UncompliantMailController
    {
        protected static IList<UncompliantMail> uncompliantsMail;
        private static readonly string csvFilename = $"{AppDomain.CurrentDomain.BaseDirectory}UncompliantMail.csv";

        static UncompliantMailController()
        {
            // Check if file exists
            if (!File.Exists(csvFilename))
            {
                Log.Debug("File does not exists! Created.");
                using (var sw = new StreamWriter(csvFilename, true))
                {
                    sw.WriteLine("COGNOME,NOME,POSTA_ELETTRONICA");
                }
            }

            using (var reader = new StreamReader(csvFilename))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<UncompliantMailMap>();
                uncompliantsMail = csv.GetRecords<UncompliantMail>().ToList();
                Log.Debug("Uncompliant found: {0}.", uncompliantsMail.Count);
            }
        }

        public static IList<UncompliantMail> List()
        {
            return uncompliantsMail.ToList();
        }
        public static UncompliantMail Get(string lastName, string firstName)
        {
            return List().FirstOrDefault(um => um.LastName == lastName && um.FirstName == firstName);
        }
    }
}
