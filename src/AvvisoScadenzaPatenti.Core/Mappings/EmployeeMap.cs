namespace AvvisoScadenzaPatenti.Core.Mappings;

using CsvHelper.Configuration;

using AvvisoScadenzaPatenti.Core.Models;

public sealed class EmployeeMap : ClassMap<Employee>
{
    public EmployeeMap()
    {
        this.Map(m => m.LastName).Name("COGNOME");
        this.Map(m => m.FirstName).Name("NOME");
        this.Map(m => m.Mail).Name("POSTA_ELETTRONICA");
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
