namespace AvvisoScadenzaPatenti.Core.Models;

public class Employee
{
    public required string LastName { get; set; }
    public required string FirstName { get; set; }
    public required string Mail { get; set; }
    public bool Warning1Day { get; set; } = false;
    public bool Warning1Week { get; set; } = false;
    public bool Warning2Weeks { get; set; } = false;
    public bool Warning1Month { get; set; } = false;
    public bool Warning2Months { get; set; } = false;

    /// <summary>
    /// Checks if at least one warning flag is currently set to true.
    /// Useful to avoid unnecessary repository updates.
    /// </summary>
    public bool HasAnyWarningActive()
    {
        return Warning1Day ||
               Warning1Week ||
               Warning2Weeks ||
               Warning1Month ||
               Warning2Months;
    }

    /// <summary>
    /// Resets all warning flags to false.
    /// Typically used when a license is renewed or far from expiration.
    /// </summary>
    public void ResetAllWarnings()
    {
        Warning1Day = false;
        Warning1Week = false;
        Warning2Weeks = false;
        Warning1Month = false;
        Warning2Months = false;
    }
}
