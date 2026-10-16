namespace RaceDayAPI.Models;

public class Registration
{
    public int RegistrationId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public int ParticipantId { get; set; }

    public string PaymentStatus { get; set; } = "Pending";
    public string? BibNumber { get; set; }
}