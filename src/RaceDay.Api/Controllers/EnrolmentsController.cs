using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>Event enrolments. Participants enrol and see their own. Organisers see enrolments for their events.</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class EnrolmentsController(RaceDayDbContext db) : RaceDayControllerBase
{
    /// <summary>
    /// Lists enrolments visible to the caller. Participants get their own enrolments.
    /// Organisers get the enrolments for events they created.
    /// </summary>
    [HttpGet("enrolments")]
    [SessionAuthorize]
    [ProducesResponseType<IReadOnlyList<EnrolmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<EnrolmentResponse>>> GetEnrolments()
    {
        var query = EnrolmentsWithDetails();

        query = CurrentRole == UserRoles.Participant
            ? query.Where(en => en.Participant.UserID == CurrentUserId)
            : query.Where(en => en.Event.Organiser.UserID == CurrentUserId);

        var enrolments = await query.OrderBy(en => en.Event.EventDate).ToListAsync();

        return Ok(enrolments.Select(EnrolmentResponse.FromEnrolment).ToList());
    }

    /// <summary>Enrols the signed-in Participant in an event, using one of that event's categories.</summary>
    /// <remarks>
    /// New enrolments start as Pending. An Organiser confirms them with PUT /api/enrolments/{id}/status.
    /// A participant can enrol in the same event only once.
    /// </remarks>
    [HttpPost("events/{eventId:int}/enrol")]
    [SessionAuthorize(UserRoles.Participant)]
    [ProducesResponseType<EnrolmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrolmentResponse>> Enrol(int eventId, [FromBody] EnrolRequest request)
    {
        var evt = await db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (evt is null)
        {
            return Problem(detail: $"Event {eventId} was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var category = await db.Categories.FirstOrDefaultAsync(c => c.CategoryID == request.CategoryId);
        if (category is null || category.EventID != eventId)
        {
            return Problem(detail: "categoryId must be one of this event's categories.", statusCode: StatusCodes.Status400BadRequest);
        }

        var participant = await db.Participants
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserID == CurrentUserId);
        if (participant is null)
        {
            return Problem(detail: "This account has no Participant profile.", statusCode: StatusCodes.Status403Forbidden);
        }

        if (await db.Enrolments.AnyAsync(en => en.ParticipantID == participant.ParticipantID && en.EventID == eventId))
        {
            return Problem(detail: "You are already enrolled in this event.", statusCode: StatusCodes.Status400BadRequest);
        }

        var enrolment = new Enrolment
        {
            ParticipantID = participant.ParticipantID,
            Participant = participant,
            EventID = evt.EventID,
            Event = evt,
            CategoryID = category.CategoryID,
            Category = category,
            Status = EnrolmentStatuses.Pending,
            RegistrationDate = DateTime.Now,
        };

        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, EnrolmentResponse.FromEnrolment(enrolment));
    }

    /// <summary>Lists every enrolment for one event. Only the Organiser who owns the event can see them.</summary>
    [HttpGet("events/{eventId:int}/enrolments")]
    [SessionAuthorize(UserRoles.Organiser)]
    [ProducesResponseType<IReadOnlyList<EnrolmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EnrolmentResponse>>> GetEventEnrolments(int eventId)
    {
        var (_, failure) = await LoadOwnedEventAsync(db, eventId);
        if (failure is not null)
        {
            return failure;
        }

        var enrolments = await EnrolmentsWithDetails()
            .Where(en => en.EventID == eventId)
            .OrderBy(en => en.Participant.User.FullName)
            .ToListAsync();

        return Ok(enrolments.Select(EnrolmentResponse.FromEnrolment).ToList());
    }

    /// <summary>Changes an enrolment's status to Pending, Confirmed or Cancelled. Only the event's Organiser can do this.</summary>
    [HttpPut("enrolments/{id:int}/status")]
    [SessionAuthorize(UserRoles.Organiser)]
    [ProducesResponseType<EnrolmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrolmentResponse>> UpdateStatus(int id, [FromBody] UpdateEnrolmentStatusRequest request)
    {
        var enrolment = await EnrolmentsWithDetails().FirstOrDefaultAsync(en => en.EnrolmentID == id);
        if (enrolment is null)
        {
            return Problem(detail: $"Enrolment {id} was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        if (enrolment.Event.Organiser.UserID != CurrentUserId)
        {
            return Problem(detail: "You can only manage enrolments for events that you created.", statusCode: StatusCodes.Status403Forbidden);
        }

        var status = request.Status.Trim();
        if (!EnrolmentStatuses.All.Contains(status, StringComparer.Ordinal))
        {
            return Problem(detail: "status must be Pending, Confirmed or Cancelled.", statusCode: StatusCodes.Status400BadRequest);
        }

        enrolment.Status = status;
        await db.SaveChangesAsync();

        return Ok(EnrolmentResponse.FromEnrolment(enrolment));
    }

    /// <summary>Enrolments with the participant, event and category loaded, ready for EnrolmentResponse.</summary>
    private IQueryable<Enrolment> EnrolmentsWithDetails() => db.Enrolments
        .Include(en => en.Participant).ThenInclude(p => p.User)
        .Include(en => en.Event).ThenInclude(e => e.Organiser)
        .Include(en => en.Category);
}
