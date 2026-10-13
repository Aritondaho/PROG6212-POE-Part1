namespace RaceDay.Api.Models;

/// <summary>A road run, walk or cycle event created and owned by an <see cref="Organiser"/>.</summary>
public class Event
{
    public int EventID { get; set; }

    public int OrganiserID { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    public string Location { get; set; } = string.Empty;

    /// <summary>Distance in kilometres. DECIMAL(5,2) in SQL Server.</summary>
    public decimal Distance { get; set; }

    /// <summary>Either Run, Walk or Cycle.</summary>
    public string EventType { get; set; } = string.Empty;

    public string? BannerImageURL { get; set; }

    public DateTime CreatedAt { get; set; }

    public Organiser Organiser { get; set; } = null!;

    public ICollection<Category> Categories { get; set; } = new List<Category>();

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
