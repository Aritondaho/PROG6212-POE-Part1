using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models;

public class ResultsPageViewModel
{
    public required OrganiserEventSummary Summary { get; init; }

    /// <summary>Active (not cancelled) entries, each with its result if one has been captured.</summary>
    public IReadOnlyList<(EnrolmentItem Enrolment, ResultItem? Result)> Rows { get; init; } = [];

    /// <summary>Results can be captured once the event has started.</summary>
    public bool CanCapture => Summary.Event.Date <= DateTime.Now;

    public int Captured => Rows.Count(r => r.Result is not null);
}
