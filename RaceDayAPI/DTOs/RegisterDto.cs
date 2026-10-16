using System.ComponentModel.DataAnnotations;
using RaceDayAPI.Models;

namespace RaceDayAPI.DTOs;

public class RegisterDto
{
    [Required]
    public string Name { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(8)]
    public string Password { get; set; } = "";

    [Range(1, 120)]
    public int Age { get; set; }

    public string Gender { get; set; } = "";
    public string ContactInfo { get; set; } = "";

    public UserRole Role { get; set; }

    // Only needed when registering as an Organiser
    public string? OrganiserCode { get; set; }
}