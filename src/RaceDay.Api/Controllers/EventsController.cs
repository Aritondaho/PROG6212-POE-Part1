using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>Public event browsing. Organiser write operations are added in the next part of this controller.</summary>
[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController(RaceDayDbContext db) : RaceDayControllerBase
{
    /// <summary>Lists events. Anyone can call this. Use the optional filters to narrow the list.</summary>
    /// <param name="type">Only events of this type: Run, Walk or Cycle.</param>
    /// <param name="date">Only events that start on this calendar day.</param>
    /// <param name="location">Only events whose location contains this text.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EventResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<EventResponse>>> GetEvents(
        [FromQuery] string? type,
        [FromQuery] DateOnly? date,
        [FromQuery] string? location)
    {
        var query = db.Events.Include(e => e.Organiser).AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!EventTypes.All.Contains(type.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                return Problem(detail: "type must be Run, Walk or Cycle.", statusCode: StatusCodes.Status400BadRequest);
            }

            var eventType = type.Trim();
            query = query.Where(e => e.EventType == eventType);
        }

        if (date is not null)
        {
            var dayStart = date.Value.ToDateTime(TimeOnly.MinValue);
            var dayEnd = dayStart.AddDays(1);
            query = query.Where(e => e.EventDate >= dayStart && e.EventDate < dayEnd);
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var term = location.Trim();
            query = query.Where(e => e.Location.Contains(term));
        }

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();

        return Ok(events.Select(EventResponse.FromEvent).ToList());
    }

    /// <summary>Returns one event by ID. Anyone can call this.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetEvent(int id)
    {
        var evt = await db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventID == id);
        if (evt is null)
        {
            return Problem(detail: $"Event {id} was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(EventResponse.FromEvent(evt));
    }

    /// <summary>Creates an event owned by the signed-in Organiser.</summary>
    [HttpPost]
    [SessionAuthorize(UserRoles.Organiser)]
    [ProducesResponseType<EventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventResponse>> CreateEvent([FromBody] EventRequest request)
    {
        if (!EventTypes.All.Contains(request.EventType.Trim(), StringComparer.Ordinal))
        {
            return Problem(detail: "eventType must be Run, Walk or Cycle.", statusCode: StatusCodes.Status400BadRequest);
        }

        var organiser = await db.Organisers.FirstOrDefaultAsync(o => o.UserID == CurrentUserId);
        if (organiser is null)
        {
            return Problem(detail: "This account has no Organiser profile.", statusCode: StatusCodes.Status403Forbidden);
        }

        var evt = new Event
        {
            OrganiserID = organiser.OrganiserID,
            Organiser = organiser,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.Date,
            Location = request.Location.Trim(),
            Distance = request.Distance,
            EventType = request.EventType.Trim(),
            BannerImageURL = request.BannerImageUrl?.Trim(),
            CreatedAt = DateTime.Now,
        };

        db.Events.Add(evt);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = evt.EventID }, EventResponse.FromEvent(evt));
    }

    /// <summary>Updates an event. Only the Organiser who created it can do this.</summary>
    /// <remarks>The body matches create. If bannerImageUrl is left out, the current banner is kept.</remarks>
    [HttpPut("{id:int}")]
    [SessionAuthorize(UserRoles.Organiser)]
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> UpdateEvent(int id, [FromBody] EventRequest request)
    {
        if (!EventTypes.All.Contains(request.EventType.Trim(), StringComparer.Ordinal))
        {
            return Problem(detail: "eventType must be Run, Walk or Cycle.", statusCode: StatusCodes.Status400BadRequest);
        }

        var (evt, failure) = await LoadOwnedEventAsync(id);
        if (failure is not null)
        {
            return failure;
        }

        evt!.Name = request.Name.Trim();
        evt.Description = request.Description.Trim();
        evt.EventDate = request.Date;
        evt.Location = request.Location.Trim();
        evt.Distance = request.Distance;
        evt.EventType = request.EventType.Trim();
        if (!string.IsNullOrWhiteSpace(request.BannerImageUrl))
        {
            evt.BannerImageURL = request.BannerImageUrl.Trim();
        }

        await db.SaveChangesAsync();

        return Ok(EventResponse.FromEvent(evt));
    }

    /// <summary>Deletes an event that the signed-in Organiser owns. Categories are removed with it.</summary>
    /// <remarks>Returns 409 if participants have enrolled, because enrolments and results must be kept.</remarks>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(UserRoles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var (evt, failure) = await LoadOwnedEventAsync(id);
        if (failure is not null)
        {
            return failure;
        }

        if (await db.Enrolments.AnyAsync(en => en.EventID == id))
        {
            return Problem(
                detail: "This event has enrolments and cannot be deleted. Cancel the enrolments first.",
                statusCode: StatusCodes.Status409Conflict);
        }

        db.Events.Remove(evt!);
        await db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Finds an event and checks that the signed-in Organiser owns it. Returns 404 when the event does not exist
    /// and 403 when another Organiser owns it.
    /// </summary>
    private async Task<(Event? Event, ActionResult? Failure)> LoadOwnedEventAsync(int id)
    {
        var evt = await db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventID == id);
        if (evt is null)
        {
            return (null, Problem(detail: $"Event {id} was not found.", statusCode: StatusCodes.Status404NotFound));
        }

        if (evt.Organiser.UserID != CurrentUserId)
        {
            return (null, Problem(detail: "You can only change events that you created.", statusCode: StatusCodes.Status403Forbidden));
        }

        return (evt, null);
    }
}
