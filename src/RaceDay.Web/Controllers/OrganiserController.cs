using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>The organiser's dashboard: their events, enrolment totals and upcoming dates.</summary>
[RequireRole(RaceDayValues.Organiser)]
public class OrganiserController(RaceDayApiClient api) : RaceDayController
{
    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        ViewData["Title"] = "Dashboard";
        var summaries = await api.GetMyEventsAsync(ct);
        return View(summaries);
    }
}
