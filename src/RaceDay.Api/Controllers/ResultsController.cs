using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>Race results. Organisers capture them after an event. Participants read their own history.</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class ResultsController(RaceDayDbContext db) : RaceDayControllerBase
{
    /// <summary>Returns the signed-in Participant's race history, newest event first.</summary>
    [HttpGet("results/me")]
    [SessionAuthorize(UserRoles.Participant)]
    [ProducesResponseType<IReadOnlyList<ResultResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ResultResponse>>> GetMyResults()
    {
        var results = await ResultsWithDetails()
            .Where(r => r.Enrolment.Participant.UserID == CurrentUserId)
            .OrderByDescending(r => r.Enrolment.Event.EventDate)
            .ToListAsync();

        var finishers = await FinisherCountsAsync(results.Select(r => r.Enrolment.EventID).Distinct());

        return Ok(results.Select(r => ResultResponse.FromResult(r, finishers.GetValueOrDefault(r.Enrolment.EventID))).ToList());
    }

    /// <summary>Lists the results for one event. Only the Organiser who owns the event can see them.</summary>
    [HttpGet("events/{eventId:int}/results")]
    [SessionAuthorize(UserRoles.Organiser)]
    [ProducesResponseType<IReadOnlyList<ResultResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ResultResponse>>> GetEventResults(int eventId)
    {
        var (_, failure) = await LoadOwnedEventAsync(db, eventId);
        if (failure is not null)
        {
            return failure;
        }

        var results = await ResultsWithDetails()
            .Where(r => r.Enrolment.EventID == eventId)
            .OrderBy(r => r.FinishPosition)
            .ToListAsync();

        var finishers = await FinisherCountsAsync([eventId]);

        return Ok(results.Select(r => ResultResponse.FromResult(r, finishers.GetValueOrDefault(eventId))).ToList());
    }

    /// <summary>Records the finish time and position for an enrolment in an event the Organiser owns.</summary>
    /// <remarks>Each enrolment can have only one result. A second attempt returns 409.</remarks>
    [HttpPost("events/{eventId:int}/results")]
    [SessionAuthorize(UserRoles.Organiser)]
    [ProducesResponseType<ResultResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultResponse>> CreateResult(int eventId, [FromBody] CreateResultRequest request)
    {
        if (ValidateFinishTime(request.FinishTime) is { } timeError)
        {
            return Problem(detail: timeError, statusCode: StatusCodes.Status400BadRequest);
        }

        var (_, failure) = await LoadOwnedEventAsync(db, eventId);
        if (failure is not null)
        {
            return failure;
        }

        var enrolment = await db.Enrolments.FirstOrDefaultAsync(en => en.EnrolmentID == request.EnrolmentId);
        if (enrolment is null || enrolment.EventID != eventId)
        {
            return Problem(detail: "enrolmentId must refer to an enrolment in this event.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (await db.Results.AnyAsync(r => r.EnrolmentID == enrolment.EnrolmentID))
        {
            return Problem(detail: "A result has already been recorded for this enrolment. Use PUT to change it.", statusCode: StatusCodes.Status409Conflict);
        }

        var result = new Result
        {
            EnrolmentID = enrolment.EnrolmentID,
            FinishTime = request.FinishTime,
            FinishPosition = request.FinishPosition,
            Status = ResultStatuses.Published,
        };

        db.Results.Add(result);
        await db.SaveChangesAsync();

        var saved = await ResultsWithDetails().FirstAsync(r => r.ResultID == result.ResultID);
        var finishers = await FinisherCountsAsync([eventId]);

        return StatusCode(StatusCodes.Status201Created, ResultResponse.FromResult(saved, finishers.GetValueOrDefault(eventId)));
    }

    /// <summary>Changes the finish time or position of an existing result. Only the event's Organiser can do this.</summary>
    [HttpPut("results/{id:int}")]
    [SessionAuthorize(UserRoles.Organiser)]
    [ProducesResponseType<ResultResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultResponse>> UpdateResult(int id, [FromBody] UpdateResultRequest request)
    {
        if (ValidateFinishTime(request.FinishTime) is { } timeError)
        {
            return Problem(detail: timeError, statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await ResultsWithDetails().FirstOrDefaultAsync(r => r.ResultID == id);
        if (result is null)
        {
            return Problem(detail: $"Result {id} was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        if (result.Enrolment.Event.Organiser.UserID != CurrentUserId)
        {
            return Problem(detail: "You can only manage results for events that you created.", statusCode: StatusCodes.Status403Forbidden);
        }

        result.FinishTime = request.FinishTime;
        result.FinishPosition = request.FinishPosition;
        await db.SaveChangesAsync();

        var finishers = await FinisherCountsAsync([result.Enrolment.EventID]);

        return Ok(ResultResponse.FromResult(result, finishers.GetValueOrDefault(result.Enrolment.EventID)));
    }

    private static string? ValidateFinishTime(TimeSpan finishTime) =>
        finishTime <= TimeSpan.Zero || finishTime >= TimeSpan.FromHours(24)
            ? "finishTime must be greater than 00:00:00 and less than 24:00:00."
            : null;

    /// <summary>Results with enrolment, event, category and participant user loaded, ready for ResultResponse.</summary>
    private IQueryable<Result> ResultsWithDetails() => db.Results
        .Include(r => r.Enrolment).ThenInclude(en => en.Event).ThenInclude(e => e.Organiser)
        .Include(r => r.Enrolment).ThenInclude(en => en.Category)
        .Include(r => r.Enrolment).ThenInclude(en => en.Participant).ThenInclude(p => p.User);

    /// <summary>Counts published results for each event, so finishing positions can show the field size.</summary>
    private async Task<Dictionary<int, int>> FinisherCountsAsync(IEnumerable<int> eventIds)
    {
        var ids = eventIds.ToList();

        return await db.Results
            .Where(r => ids.Contains(r.Enrolment.EventID) && r.Status == ResultStatuses.Published)
            .GroupBy(r => r.Enrolment.EventID)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count);
    }
}
