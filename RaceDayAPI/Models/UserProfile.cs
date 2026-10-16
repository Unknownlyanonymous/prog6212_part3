using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models;

public enum UserRole
{
    Organiser,
    Participant
}

public class UserProfile
{
    [Key]
    public int UserId { get; set; }

    [Required]
    public string Name { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    public int Age { get; set; }
    public string Gender { get; set; } = "";
    public string ContactInfo { get; set; } = "";
    public UserRole Role { get; set; }
}