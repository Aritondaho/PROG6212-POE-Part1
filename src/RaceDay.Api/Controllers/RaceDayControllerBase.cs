using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>Shared helpers for RaceDay controllers: read the signed-in user from the session.</summary>
public abstract class RaceDayControllerBase : ControllerBase
{
    /// <summary>The signed-in user's ID. Only meaningful inside actions protected by <see cref="SessionAuthorizeAttribute"/>.</summary>
    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    /// <summary>The signed-in user's role ("Organiser" or "Participant").</summary>
    protected string CurrentRole => HttpContext.Session.GetString(SessionKeys.Role) ?? string.Empty;
}
