using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.DTOs;

public class CategoryDto
{
    [Required]
    public string Name { get; set; } = "";       // e.g. "Under 20", "Senior", "10km"

    [Range(0.1, 1000)]
    public double Distance { get; set; }         // in km
}