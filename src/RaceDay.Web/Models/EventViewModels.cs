using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models;

/// <summary>Filters on the events page. Type, date and location are sent to the API; name search and "past events" only narrow what is shown.</summary>
public class EventFilter
{
    public string? Search { get; set; }

    public string? Type { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? Date { get; set; }

    public string? Location { get; set; }

    public bool IncludePast { get; set; }

    public bool IsActive =>
        !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(Type) ||
        Date is not null || !string.IsNullOrWhiteSpace(Location) || IncludePast;
}

public class EventListViewModel
{
    public EventFilter Filter { get; init; } = new();

    public IReadOnlyList<EventItem> Events { get; init; } = [];

    /// <summary>Events the signed-in participant has already entered, so their cards can say so.</summary>
    public IReadOnlySet<int> EnteredEventIds { get; init; } = new HashSet<int>();
}

public class EventDetailsViewModel
{
    public required EventItem Event { get; init; }

    public IReadOnlyList<CategoryItem> Categories { get; init; } = [];

    /// <summary>The signed-in participant's enrolment in this event, if any.</summary>
    public EnrolmentItem? MyEnrolment { get; init; }

    [Display(Name = "Category")]
    [Required(ErrorMessage = "Choose a category to enter.")]
    public int? CategoryId { get; set; }

    public bool IsUpcoming => Infrastructure.Ui.IsUpcoming(Event.Date);
}
