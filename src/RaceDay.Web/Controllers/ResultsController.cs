using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organisers capture and publish finish times and positions after their event.</summary>
public class ResultsController(RaceDayApiClient api) : OrganiserEventControllerBase(api)
{
    private static readonly string[] TimeFormats = [@"h\:mm\:ss", @"hh\:mm\:ss", @"h\:mm", @"hh\:mm"];

    [HttpGet]
    public async Task<IActionResult> Index(int eventId, CancellationToken ct)
    {
        var summary = await FindOwnEventAsync(eventId, ct);
        if (summary is null)
        {
            return NotFoundPage();
        }

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId, ct);
        var results = (await Api.GetEventResultsAsync(eventId, ct)).ToDictionary(r => r.EnrolmentID);

        var rows = enrolments
            .Where(en => en.Status != "Cancelled" || results.ContainsKey(en.EnrolmentID))
            .Select(en => (en, results.GetValueOrDefault(en.EnrolmentID)))
            .OrderBy(row => row.Item2?.FinishPosition ?? int.MaxValue)
            .ThenBy(row => row.en.ParticipantName)
            .ToList();

        ViewData["Title"] = $"Results · {summary.Event.Name}";
        return View(new ResultsPageViewModel { Summary = summary, Rows = rows });
    }

    /// <summary>Creates the result for an entry, or updates it if one was already captured.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int eventId, int enrolmentId, int? resultId, string? finishTime, int? finishPosition, CancellationToken ct)
    {
        if (!TryParseFinishTime(finishTime, out var time))
        {
            FlashError("Enter the finish time as h:mm:ss, for example 1:52:07.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        if (finishPosition is null or < 1)
        {
            FlashError("Enter a finishing position of 1 or more.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        try
        {
            var saved = resultId is null
                ? await Api.CreateResultAsync(eventId, new CreateResultBody(enrolmentId, time, finishPosition.Value), ct)
                : await Api.UpdateResultAsync(resultId.Value, new UpdateResultBody(time, finishPosition.Value), ct);

            FlashSuccess($"Result published for {saved.ParticipantName}: {Infrastructure.Ui.Ordinal(saved.FinishPosition)} in {Infrastructure.Ui.FinishTime(saved.FinishTime)}.");
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            FlashError(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private static bool TryParseFinishTime(string? text, out TimeSpan time)
    {
        time = default;
        return !string.IsNullOrWhiteSpace(text) &&
               TimeSpan.TryParseExact(text.Trim(), TimeFormats, CultureInfo.InvariantCulture, out time) &&
               time > TimeSpan.Zero;
    }
}
