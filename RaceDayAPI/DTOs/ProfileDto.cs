namespace RaceDayAPI.DTOs;

public class ProfileDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public string Gender { get; set; } = "";
    public string ContactInfo { get; set; } = "";
    public string Role { get; set; } = "";
}