namespace RaceDay.Api.Models;

/// <summary>Profile data for a user with the Participant role. One-to-one with <see cref="User"/>.</summary>
public class Participant
{
    public int ParticipantID { get; set; }

    public int UserID { get; set; }

    public DateOnly DateOfBirth { get; set; }

    /// <summary>Filled in by the Part 3 profile-picture feature. Null until then.</summary>
    public string? ProfilePictureURL { get; set; }

    public User User { get; set; } = null!;

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
