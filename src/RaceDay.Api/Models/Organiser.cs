namespace RaceDay.Api.Models;

/// <summary>Profile data for a user with the Organiser role. One-to-one with <see cref="User"/>.</summary>
public class Organiser
{
    public int OrganiserID { get; set; }

    public int UserID { get; set; }

    public string OrganisationName { get; set; } = string.Empty;

    public string ContactNumber { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
