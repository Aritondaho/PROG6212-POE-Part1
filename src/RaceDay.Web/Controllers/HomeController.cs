using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Web.Controllers;

/// <summary>The site root sends each role to its own home page.</summary>
public class HomeController : RaceDayController
{
    public IActionResult Index() => CurrentUser?.IsOrganiser == true
        ? RedirectToAction("Dashboard", "Organiser")
        : RedirectToAction("Index", "Events");
}
