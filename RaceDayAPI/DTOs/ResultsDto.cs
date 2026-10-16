using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.DTOs;

public class ResultDto
{
    [Required]
    public int RegistrationId { get; set; }

    [Required]
    public string FinishTime { get; set; } = "";     // format hh:mm:ss, e.g. "00:52:30"

    [Range(1, 100000)]
    public int Position { get; set; }
}