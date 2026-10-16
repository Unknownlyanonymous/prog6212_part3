namespace RaceDayAPI.Models;

public class Route
{
    public int RouteId { get; set; }
    public int EventId { get; set; }

    public double Distance { get; set; }
    public string Map { get; set; } = "";
    public double Elevation { get; set; }
}