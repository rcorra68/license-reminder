namespace avviso_scadenza_patenti.Entities
{
    using System;
    
    public class License
    {
        public string Office { get; set; }
        public string LicenseType { get; set; }
        public string Category { get; set; }
        public string LicenseNumber { get; set; }
        public string CardNumber { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string Status { get; set; }
    }
}
