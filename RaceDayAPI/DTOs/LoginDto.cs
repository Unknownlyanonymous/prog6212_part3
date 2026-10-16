using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.DTOs;

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}