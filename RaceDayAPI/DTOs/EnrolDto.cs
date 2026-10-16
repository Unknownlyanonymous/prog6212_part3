using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.DTOs;

public class EnrolDto
{
    [Required]
    public int EventId { get; set; }

    [Required]
    public int CategoryId { get; set; }
}