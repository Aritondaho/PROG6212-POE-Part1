using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

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
}
