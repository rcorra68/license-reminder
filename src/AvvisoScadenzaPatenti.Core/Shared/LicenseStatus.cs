namespace AvvisoScadenzaPatenti.Core.Shared;

/// <summary>
/// Known values of the STATO column in licenses.csv.
/// </summary>
public static class LicenseStatus
{
    /// <summary>
    /// The license has been physically destroyed (e.g. holder retired).
    /// Destroyed licenses are excluded from expiration handling.
    /// </summary>
    public const string Destroyed = "DISTRUTTA";
}
