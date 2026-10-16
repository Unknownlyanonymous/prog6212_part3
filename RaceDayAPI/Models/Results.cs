namespace RaceDayAPI.Models;

public class Result
{
    public int ResultId { get; set; }
    public int RegistrationId { get; set; }

    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
    public string Pace { get; set; } = "";
}
