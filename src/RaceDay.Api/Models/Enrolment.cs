namespace RaceDay.Api.Models;

/// <summary>
/// Links a <see cref="Participant"/> to an <see cref="Event"/> through a chosen <see cref="Category"/>.
/// A participant can enrol in each event only once (UNIQUE ParticipantID + EventID).
/// </summary>
public class Enrolment
{
    public int EnrolmentID { get; set; }

    public int ParticipantID { get; set; }

    public int EventID { get; set; }

    public int CategoryID { get; set; }

    public DateTime RegistrationDate { get; set; }

    /// <summary>Pending, Confirmed or Cancelled. New enrolments start as Pending.</summary>
    public string Status { get; set; } = EnrolmentStatuses.Pending;

    public Participant Participant { get; set; } = null!;

    public Event Event { get; set; } = null!;

    public Category Category { get; set; } = null!;

    public Result? Result { get; set; }
}
