using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models;

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required]
    public string Name { get; set; } = "";

    public double Distance { get; set; }
}