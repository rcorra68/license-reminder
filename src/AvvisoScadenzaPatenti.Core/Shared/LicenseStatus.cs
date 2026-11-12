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
    public const string Destroyed = "PATENTE DISTRUTTA";

    /// <summary>
    /// Label used by v3.11.0. Still recognized so rows written before the
    /// label change keep being excluded from expiration handling.
    /// </summary>
    public const string LegacyDestroyed = "DISTRUTTA";

    /// <summary>
    /// True when the given STATO value marks a destroyed license
    /// (case-insensitive, surrounding whitespace ignored).
    /// </summary>
    public static bool IsDestroyed(string? status)
    {
        var value = status?.Trim();

        return string.Equals(value, Destroyed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, LegacyDestroyed, StringComparison.OrdinalIgnoreCase);
    }
}
