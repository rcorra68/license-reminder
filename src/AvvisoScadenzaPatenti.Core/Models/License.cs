namespace AvvisoScadenzaPatenti.Core.Models;

using System;

public class License
{
    public string Office { get; set; } = null!;
    public string LicenseType { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string LicenseNumber { get; set; } = null!;
    public string CardNumber { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public DateTime ReleaseDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Status { get; set; } = null!;
}
