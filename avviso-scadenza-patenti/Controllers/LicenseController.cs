namespace avviso_scadenza_patenti.Controllers
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using avviso_scadenza_patenti.Entities;

    using CsvHelper;

    using Serilog;

    internal class LicenseController
    {
        protected static IList<License> licenses;
        private static readonly string csvFilename = $"{AppDomain.CurrentDomain.BaseDirectory}SRLArchivioPatenti.csv";

        static LicenseController()
        {
            // Check if file exists
            if (!File.Exists(csvFilename))
            {
                Log.Debug("File does not exists! Created.");
                using (var sw = new StreamWriter(csvFilename, true))
                {
                    sw.WriteLine("SEDE,TIPO_PATENTE,CATEGORIA,NUMERO_PATENTE,NUMERO_CARD,COGNOME,NOME,DATA_RILASCIO,DATA_SCADENZA,STATO");
                }
            }

            using (var reader = new StreamReader(csvFilename))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<LicenseMap>();
                licenses = csv.GetRecords<License>().ToList();
                Log.Debug("Licenses found: {0}.", licenses.Count);
            }
        }

        public static IList<License> List()
        {
            return licenses.ToList();
        }
    }
}
