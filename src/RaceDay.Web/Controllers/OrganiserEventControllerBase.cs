using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Base for organiser pages that work on one event (categories, enrolments, results).
/// Pages only open for events in the organiser's own list; the API enforces ownership again on every change.
/// </summary>
[RequireRole(RaceDayValues.Organiser)]
public abstract class OrganiserEventControllerBase(RaceDayApiClient api) : RaceDayController
{
    protected RaceDayApiClient Api { get; } = api;

    /// <summary>The organiser's own event with its counts, or null if it does not exist or belongs to someone else.</summary>
    protected async Task<OrganiserEventSummary?> FindOwnEventAsync(int eventId, CancellationToken ct) =>
        (await Api.GetMyEventsAsync(ct)).FirstOrDefault(s => s.Event.EventID == eventId);

    protected ViewResult NotFoundPage()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("NotFound");
    }
}
