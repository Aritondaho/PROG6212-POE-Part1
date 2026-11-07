using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Public event pages: the upcoming events list with filters, and the event detail page.
/// Anyone can browse. Only signed-in participants see their entry status and the Enter button.
/// </summary>
public class EventsController(RaceDayApiClient api) : RaceDayController
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] EventFilter filter, CancellationToken ct)
    {
        ViewData["Title"] = "Upcoming events";

        // An unknown type would make the API return 400, so ignore it instead of showing an error.
        if (!string.IsNullOrWhiteSpace(filter.Type) && !Models.Api.RaceDayValues.EventTypes.Contains(filter.Type))
        {
            filter.Type = null;
        }

        var events = await api.GetEventsAsync(filter.Type, filter.Date, filter.Location, ct);

        IEnumerable<Models.Api.EventItem> visible = events;
        if (!filter.IncludePast && filter.Date is null)
        {
            visible = visible.Where(e => Ui.IsUpcoming(e.Date));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            visible = visible.Where(e =>
                e.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.OrganisationName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var entered = new HashSet<int>();
        if (CurrentUser?.IsParticipant == true)
        {
            entered = (await api.GetEnrolmentsAsync(ct))
                .Where(en => en.Status != "Cancelled")
                .Select(en => en.EventID)
                .ToHashSet();
        }

        return View(new EventListViewModel
        {
            Filter = filter,
            Events = visible.OrderBy(e => e.Date).ToList(),
            EnteredEventIds = entered,
        });
    }
}
