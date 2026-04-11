namespace AvvisoScadenzaPatenti.Core.Models;

public class DailyReport
{
    public DateTime ExecutionDate { get; set; }
    public int ProcessedEmployees { get; set; }
    public int EmailsSent { get; set; }
    public int Errors { get; set; }
    public TimeSpan ExecutionTime { get; set; }
}