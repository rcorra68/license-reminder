namespace AvvisoScadenzaPatenti.Core.Mappings;

using System.Globalization;

using CsvHelper.Configuration;

using AvvisoScadenzaPatenti.Core.Models;

public sealed class LicenseMap : ClassMap<License>
{
    public LicenseMap()
    {
        string dateFormat = "dd/MM/yyyy";
        CultureInfo cultureInfo = new CultureInfo("it-IT");

        this.Map(m => m.Office).Name("SEDE");
        this.Map(m => m.LicenseType).Name("TIPO_PATENTE");
        this.Map(m => m.Category).Name("CATEGORIA");
        this.Map(m => m.LicenseNumber).Name("NUMERO_PATENTE");
        this.Map(m => m.CardNumber).Name("NUMERO_CARD");
        this.Map(m => m.LastName).Name("COGNOME");
        this.Map(m => m.FirstName).Name("NOME");
        this.Map(m => m.ReleaseDate).Name("DATA_RILASCIO")
            .TypeConverterOption.Format(dateFormat)
            .TypeConverterOption.CultureInfo(cultureInfo);
        this.Map(m => m.ExpiryDate).Name("DATA_SCADENZA")
            .TypeConverterOption.Format(dateFormat)
            .TypeConverterOption.CultureInfo(cultureInfo);
        this.Map(m => m.Status).Name("STATO");
    }
}
