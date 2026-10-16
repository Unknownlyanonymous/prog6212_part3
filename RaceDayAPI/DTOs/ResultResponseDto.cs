namespace RaceDayAPI.DTOs;

public class ResultResponseDto
{
    public int ResultId { get; set; }
    public int RegistrationId { get; set; }
    public string EventName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
    public string Pace { get; set; } = "";
}