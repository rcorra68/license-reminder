namespace AvvisoScadenzaPatenti.Core.Models;

public class Employee
{
    public string LastName { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string Mail { get; set; } = null!;
    public Boolean Warning2Months { get; set; }
    public Boolean Warning1Month { get; set; }
    public Boolean Warning2Weeks { get; set; }
    public Boolean Warning1Week { get; set; }
    public Boolean Warning1Day { get; set; }
}
