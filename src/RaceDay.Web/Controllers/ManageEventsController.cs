using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Organiser event management: create, edit and delete events and upload a banner image.
/// The API checks ownership on every change; the MVC side only lists the organiser's own events.
/// </summary>
[RequireRole(RaceDayValues.Organiser)]
public class ManageEventsController(RaceDayApiClient api) : RaceDayController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Manage events";
        return View(await api.GetMyEventsAsync(ct));
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "Create event";
        return View("Form", new EventFormViewModel { Date = DateTime.Today.AddDays(30).AddHours(6) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Create event";
        model.EventId = null;
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        EventItem created;
        try
        {
            created = await api.CreateEventAsync(model.ToBody(), ct);
        }
        catch (ApiException ex)
        {
            AddApiError(ex);
            return View("Form", model);
        }

        if (!await TryUploadBannerAsync(created.EventID, model.Banner, ct))
        {
            // The event exists; let the organiser retry the banner from the edit page.
            return RedirectToAction(nameof(Edit), new { id = created.EventID });
        }

        FlashSuccess($"{created.Name} is live. Now add the categories participants can enter.");
        return RedirectToAction("Index", "Categories", new { eventId = created.EventID });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var evt = await FindOwnEventAsync(id, ct);
        if (evt is null)
        {
            return NotFoundPage();
        }

        ViewData["Title"] = $"Edit {evt.Name}";
        return View("Form", EventFormViewModel.From(evt));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = $"Edit {model.Name}";
        model.EventId = id;
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        try
        {
            await api.UpdateEventAsync(id, model.ToBody(), ct);
        }
        catch (ApiException ex)
        {
            AddApiError(ex);
            return View("Form", model);
        }

        if (!await TryUploadBannerAsync(id, model.Banner, ct))
        {
            return RedirectToAction(nameof(Edit), new { id });
        }

        FlashSuccess("Event updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var summary = (await api.GetMyEventsAsync(ct)).FirstOrDefault(s => s.Event.EventID == id);
        if (summary is null)
        {
            return NotFoundPage();
        }

        ViewData["Title"] = $"Delete {summary.Event.Name}";
        return View(summary);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        try
        {
            await api.DeleteEventAsync(id, ct);
            FlashSuccess("Event deleted.");
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            // For example 409: participants have already entered, so the API keeps the event.
            FlashError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Sends the banner to the API, which stores it in Azure Blob Storage. Returns false and flashes the reason on failure.</summary>
    private async Task<bool> TryUploadBannerAsync(int eventId, IFormFile? banner, CancellationToken ct)
    {
        if (banner is null || banner.Length == 0)
        {
            return true;
        }

        try
        {
            await api.UploadBannerAsync(eventId, banner, ct);
            return true;
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            FlashError($"The event was saved, but the banner was not uploaded: {ex.Message}");
            return false;
        }
    }

    private async Task<EventItem?> FindOwnEventAsync(int id, CancellationToken ct) =>
        (await api.GetMyEventsAsync(ct)).FirstOrDefault(s => s.Event.EventID == id)?.Event;

    private ViewResult NotFoundPage()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("NotFound");
    }
}
