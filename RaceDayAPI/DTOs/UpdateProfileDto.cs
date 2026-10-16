using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.DTOs;

public class UpdateProfileDto
{
    [Required]
    public string Name { get; set; } = "";

    [Range(1, 120)]
    public int Age { get; set; }

    public string Gender { get; set; } = "";
    public string ContactInfo { get; set; } = "";
}