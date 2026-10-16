namespace RaceDayAPI.DTOs;

public class RegistrationResponseDto
{
    public int RegistrationId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public string? BibNumber { get; set; }
}