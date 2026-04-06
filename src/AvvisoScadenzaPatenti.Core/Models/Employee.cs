namespace AvvisoScadenzaPatenti.Core.Models;

public class Employee
{
    public required string LastName { get; set; }
    public required string FirstName { get; set; }
    public required string Mail { get; set; }
    public Boolean Warning2Months { get; set; } = false;
    public Boolean Warning1Month { get; set; } = false;
    public Boolean Warning2Weeks { get; set; } = false;
    public Boolean Warning1Week { get; set; } = false;
    public Boolean Warning1Day { get; set; } = false;
}
