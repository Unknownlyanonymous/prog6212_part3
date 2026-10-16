using System.ComponentModel.DataAnnotations;
using RaceDayAPI.Models;

namespace RaceDayAPI.DTOs;

public class EventDto
{
    [Required]
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public string Location { get; set; } = "";

    public EventType EventType { get; set; }
}