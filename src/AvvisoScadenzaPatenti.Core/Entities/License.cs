namespace AvvisoScadenzaPatenti.Core.Entities;

using System;

using AvvisoScadenzaPatenti.Core.Shared;

public class License
{
    public string Office { get; set; } = null!;
    public string LicenseType { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string LicenseNumber { get; set; } = null!;
    public string CardNumber { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public DateTime? ReleaseDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Status { get; set; } = null!;

    /// <summary>
    /// True when the license has been marked as destroyed (STATO = PATENTE DISTRUTTA).
    /// Destroyed licenses are no longer subject to expiration handling.
    /// </summary>
    public bool IsDestroyed() => LicenseStatus.IsDestroyed(Status);
}
