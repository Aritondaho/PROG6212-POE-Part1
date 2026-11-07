using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Shared helpers for RaceDay MVC controllers.</summary>
public abstract class RaceDayController : Controller
{
    /// <summary>The signed-in user. Only null on pages that allow visitors.</summary>
    protected CurrentUser? CurrentUser => HttpContext.GetCurrentUser();

    protected void FlashSuccess(string message) => TempData[FlashMessages.Success] = message;

    protected void FlashError(string message) => TempData[FlashMessages.Error] = message;

    /// <summary>
    /// Shows an API error on the form being redisplayed. 401 is not handled here: it is rethrown
    /// so <see cref="ApiExceptionFilter"/> can send the user to the login page.
    /// </summary>
    protected void AddApiError(ApiException ex)
    {
        if (ex is ApiUnauthorizedException)
        {
            throw ex;
        }

        ModelState.AddModelError(string.Empty, ex.Message);
    }
}
