namespace RaceDay.Api.Models;

/// <summary>An age or distance category that belongs to one event (for example "Senior" or "10km").</summary>
public class Category
{
    public int CategoryID { get; set; }

    public int EventID { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    public Event Event { get; set; } = null!;

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
