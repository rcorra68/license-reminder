namespace AvvisoScadenzaPatenti.Core.Mappings;

using CsvHelper.Configuration;

using AvvisoScadenzaPatenti.Core.Models;

public sealed class UncompliantMailMap : ClassMap<UncompliantMail>
{
    public UncompliantMailMap()
    {
        this.Map(m => m.LastName).Name("COGNOME");
        this.Map(m => m.FirstName).Name("NOME");
        this.Map(m => m.Mail).Name("POSTA_ELETTRONICA");
    }
}
