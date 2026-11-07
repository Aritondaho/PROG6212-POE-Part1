using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Web.Controllers;

/// <summary>The site root, plus the friendly error pages used instead of raw error output.</summary>
public class HomeController(ILogger<HomeController> logger) : RaceDayController
{
    /// <summary>Sends each role to its own home page.</summary>
    public IActionResult Index() => CurrentUser?.IsOrganiser == true
        ? RedirectToAction("Dashboard", "Organiser")
        : RedirectToAction("Index", "Events");

    /// <summary>Shown by UseExceptionHandler for any unhandled exception. The exception is logged, never displayed.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var failure = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (failure is not null)
        {
            logger.LogError(failure.Error, "Unhandled exception on {Path}", failure.Path);
        }

        ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View();
    }

    /// <summary>Shown for error status codes without a body, such as an unknown URL (404) or a bad form post (400).</summary>
    [Route("/Home/Status/{code:int}")]
    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        if (code == StatusCodes.Status404NotFound)
        {
            return View("NotFound");
        }

        ViewData["Message"] = code == StatusCodes.Status400BadRequest
            ? "That form could not be accepted. It may have expired; please reload the page and try again."
            : null;
        return View("Error");
    }
}
