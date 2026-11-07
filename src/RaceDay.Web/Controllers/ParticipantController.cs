using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>The participant's personal pages: the events they entered and their race results.</summary>
[RequireRole(RaceDayValues.Participant)]
public class ParticipantController(RaceDayApiClient api) : RaceDayController
{
    [HttpGet]
    public async Task<IActionResult> MyEvents(CancellationToken ct)
    {
        ViewData["Title"] = "My events";
        var enrolments = await api.GetEnrolmentsAsync(ct);
        return View(enrolments.OrderBy(en => en.EventDate).ToList());
    }

    /// <summary>Race history: every published result for events the participant entered, newest first.</summary>
    [HttpGet]
    public async Task<IActionResult> MyResults(CancellationToken ct)
    {
        ViewData["Title"] = "My results";
        var results = await api.GetMyResultsAsync(ct);
        return View(results.Where(r => r.Status == "Published").OrderByDescending(r => r.EventDate).ToList());
    }
}
