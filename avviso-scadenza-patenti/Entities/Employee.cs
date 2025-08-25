namespace avviso_scadenza_patenti.Entities
{
    public class Employee
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Mail { get; set; }
        public Boolean Warning2Months { get; set; }
        public Boolean Warning1Month { get; set; }
        public Boolean Warning2Weeks { get; set; }
        public Boolean Warning1Week { get; set; }
        public Boolean Warning1Day { get; set; }
    }
}
