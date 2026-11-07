using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organisers see everyone who entered one of their events and confirm or cancel entries.</summary>
public class EnrolmentsController(RaceDayApiClient api) : OrganiserEventControllerBase(api)
{
    [HttpGet]
    public async Task<IActionResult> Index(int eventId, string? status, string? category, CancellationToken ct)
    {
        var summary = await FindOwnEventAsync(eventId, ct);
        if (summary is null)
        {
            return NotFoundPage();
        }

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId, ct);

        ViewData["Title"] = $"Enrolments · {summary.Event.Name}";
        return View(new EnrolmentsPageViewModel
        {
            Summary = summary,
            All = enrolments,
            StatusFilter = status,
            CategoryFilter = category,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int eventId, int id, string status, CancellationToken ct)
    {
        try
        {
            var updated = await Api.UpdateEnrolmentStatusAsync(id, status, ct);
            FlashSuccess($"{updated.ParticipantName}'s entry is now {updated.Status.ToLowerInvariant()}.");
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            FlashError(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    /// <summary>Confirms every pending entry for the event in one go.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmAllPending(int eventId, CancellationToken ct)
    {
        var pending = (await Api.GetEventEnrolmentsAsync(eventId, ct)).Where(en => en.Status == "Pending").ToList();
        foreach (var enrolment in pending)
        {
            await Api.UpdateEnrolmentStatusAsync(enrolment.EnrolmentID, "Confirmed", ct);
        }

        FlashSuccess(pending.Count == 0 ? "There were no pending entries." : $"Confirmed {pending.Count} pending entr{(pending.Count == 1 ? "y" : "ies")}.");
        return RedirectToAction(nameof(Index), new { eventId });
    }
}
