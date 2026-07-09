namespace AvvisoScadenzaPatenti.Core.Mappings;

using AvvisoScadenzaPatenti.Core.Entities;
using CsvHelper.Configuration;
using System.Globalization;

public sealed class EmployeeMap : ClassMap<Employee>
{
    public EmployeeMap()
    {
        this.Map(m => m.LastName).Name("COGNOME");
        this.Map(m => m.FirstName).Name("NOME");
        this.Map(m => m.Mail).Name("POSTA_ELETTRONICA");
        this.Map(m => m.BirthDate).Name("DATA_NASCITA")
            .TypeConverterOption.Format("dd/MM/yyyy")
            .TypeConverterOption.CultureInfo(new CultureInfo("it-IT"))
            .Optional();
        this.Map(m => m.Warning2Months).Name("DUE_MESI")
            .Default("N")
            .TypeConverterOption.BooleanValues(true, true, "Y")
            .TypeConverterOption.BooleanValues(false, true, "N");
        this.Map(m => m.Warning1Month).Name("UN_MESE")
            .Default("N")
            .TypeConverterOption.BooleanValues(true, true, "Y")
            .TypeConverterOption.BooleanValues(false, true, "N");
        this.Map(m => m.Warning2Weeks).Name("DUE_SETTIMANE")
            .Default("N")
            .TypeConverterOption.BooleanValues(true, true, "Y")
            .TypeConverterOption.BooleanValues(false, true, "N");
        this.Map(m => m.Warning1Week).Name("UNA_SETTIMANA")
            .Default("N")
            .TypeConverterOption.BooleanValues(true, true, "Y")
            .TypeConverterOption.BooleanValues(false, true, "N");
        this.Map(m => m.Warning1Day).Name("UN_GIORNO")
            .Default("N")
            .TypeConverterOption.BooleanValues(true, true, "Y")
            .TypeConverterOption.BooleanValues(false, true, "N");
    }
}
