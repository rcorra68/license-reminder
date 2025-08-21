namespace avviso_scadenza_patenti.Entities
{
    using System;

    internal class Patente
    {
        string Sede { get; set; }
        string TipoPatente { get; set; }
        string Categoria { get; set; }
        string NumeroPatente { get; set; }
        string NumeroCard { get; set; }
        string Cognome { get; set; }
        string Nome { get; set; }
        DateTime DataRilascio { get; set; }
        DateTime DataScadenza { get; set; }
        string Stato { get; set; }
    }
}
