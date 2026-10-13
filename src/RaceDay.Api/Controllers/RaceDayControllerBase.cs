using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>Shared helpers for RaceDay controllers: read the signed-in user from the session and check event ownership.</summary>
public abstract class RaceDayControllerBase : ControllerBase
{
    /// <summary>The signed-in user's ID. Only meaningful inside actions protected by <see cref="SessionAuthorizeAttribute"/>.</summary>
    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    /// <summary>The signed-in user's role ("Organiser" or "Participant").</summary>
    protected string CurrentRole => HttpContext.Session.GetString(SessionKeys.Role) ?? string.Empty;

    /// <summary>
    /// Finds an event and checks that the signed-in Organiser owns it. Returns 404 when the event does not exist
    /// and 403 when another Organiser owns it. Use it in actions protected by <see cref="SessionAuthorizeAttribute"/>.
    /// </summary>
    protected async Task<(Event? Event, ActionResult? Failure)> LoadOwnedEventAsync(RaceDayDbContext db, int eventId)
    {
        var evt = await db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventID == eventId);
        if (evt is null)
        {
            return (null, Problem(detail: $"Event {eventId} was not found.", statusCode: StatusCodes.Status404NotFound));
        }

        if (evt.Organiser.UserID != CurrentUserId)
        {
            return (null, Problem(detail: "You can only manage events that you created.", statusCode: StatusCodes.Status403Forbidden));
        }

        return (evt, null);
    }
}
