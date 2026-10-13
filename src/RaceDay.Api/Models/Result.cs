namespace RaceDay.Api.Models;

/// <summary>The finish time and position captured by an Organiser for one enrolment. At most one per enrolment.</summary>
public class Result
{
    public int ResultID { get; set; }

    public int EnrolmentID { get; set; }

    /// <summary>Finish time as a TIME(0) value, always less than 24 hours.</summary>
    public TimeSpan FinishTime { get; set; }

    public int FinishPosition { get; set; }

    /// <summary>Published, Pending or Disqualified.</summary>
    public string Status { get; set; } = ResultStatuses.Published;

    public Enrolment Enrolment { get; set; } = null!;
}
