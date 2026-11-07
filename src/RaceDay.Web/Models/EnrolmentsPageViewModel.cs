using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models;

public class EnrolmentsPageViewModel
{
    public required OrganiserEventSummary Summary { get; init; }

    public IReadOnlyList<EnrolmentItem> All { get; init; } = [];

    public string? StatusFilter { get; init; }

    public string? CategoryFilter { get; init; }

    public IEnumerable<EnrolmentItem> Visible => All
        .Where(en => string.IsNullOrEmpty(StatusFilter) || en.Status == StatusFilter)
        .Where(en => string.IsNullOrEmpty(CategoryFilter) || en.CategoryName == CategoryFilter)
        .OrderBy(en => en.ParticipantName);

    public IEnumerable<string> CategoryNames => All.Select(en => en.CategoryName).Distinct().Order();

    public int Count(string status) => All.Count(en => en.Status == status);
}
