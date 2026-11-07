using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Register, log in and log out. Every step goes through the API's /api/auth endpoints.</summary>
public class AccountController(RaceDayApiClient api, ILogger<AccountController> logger) : RaceDayController
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (CurrentUser is not null)
        {
            return RedirectToHome(CurrentUser.Role);
        }

        ViewData["Title"] = "Log in";
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Log in";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var profile = await SignInAsync(model.Email, model.Password, ct);
            FlashSuccess($"Welcome back, {profile.FullName.Split(' ')[0]}!");

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToHome(profile.Role);
        }
        catch (ApiException ex)
        {
            // A 401 here means wrong credentials, not an expired session, so show it on the form.
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (CurrentUser is not null)
        {
            return RedirectToHome(CurrentUser.Role);
        }

        ViewData["Title"] = "Join RaceDay";
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "Join RaceDay";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var isOrganiser = model.Role == RaceDayValues.Organiser;
        try
        {
            await api.RegisterAsync(new RegisterBody(
                model.Email,
                model.Password,
                model.FullName,
                model.Role,
                isOrganiser ? model.OrganisationName : null,
                isOrganiser ? model.ContactNumber : null,
                isOrganiser ? null : model.DateOfBirth), ct);
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        // The API's register endpoint does not start a session, so sign the new user in straight away.
        var profile = await SignInAsync(model.Email, model.Password, ct);
        FlashSuccess(isOrganiser
            ? "Your organiser account is ready. Create your first event to get started."
            : "Welcome to RaceDay! Pick an event and enter your first race.");
        return RedirectToHome(profile.Role);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (CurrentUser is not null)
        {
            try
            {
                await api.LogoutAsync(ct);
            }
            catch (Exception ex) when (ex is ApiException or HttpRequestException)
            {
                // The API session may already have expired. The MVC session is cleared either way.
                logger.LogInformation(ex, "API logout failed; clearing the local session anyway");
            }
        }

        HttpContext.Session.Clear();
        Response.Cookies.Delete(SessionKeys.CookieName);
        TempData[FlashMessages.Info] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        ViewData["Title"] = "Access denied";
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    /// <summary>Starts a fresh API session and copies the user's ID, role and name into the MVC session.</summary>
    private async Task<UserProfile> SignInAsync(string email, string password, CancellationToken ct)
    {
        // Drop any previous user's data (including their API cookie) before signing in.
        HttpContext.Session.Clear();
        var profile = await api.LoginAsync(new LoginBody(email, password), ct);
        HttpContext.Session.SignIn(profile);
        return profile;
    }

    private IActionResult RedirectToHome(string role) => role == RaceDayValues.Organiser
        ? RedirectToAction("Dashboard", "Organiser")
        : RedirectToAction("Index", "Events");
}
