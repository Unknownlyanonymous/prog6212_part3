using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models;

public enum EventType
{
    Run,
    Walk,
    Cycle
}

public class Event
{
    public int EventId { get; set; }

    [Required]
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";
    public DateTime Date { get; set; }
    public string Location { get; set; } = "";
    public EventType EventType { get; set; }

    public List<Category> Categories { get; set; } = new();
    public List<Route> Routes { get; set; } = new();
}