using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>View and update your own profile. Participants can also upload a profile picture.</summary>
[RequireRole]
public class ProfileController(RaceDayApiClient api) : RaceDayController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "My profile";
        var profile = await api.GetProfileAsync(ct);
        return View(ProfileViewModel.From(profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model, CancellationToken ct)
    {
        ViewData["Title"] = "My profile";

        // The role and picture come from the API, never from the posted form.
        var current = await api.GetProfileAsync(ct);
        model.Role = current.Role;
        model.ProfilePictureUrl = current.ProfilePictureUrl;
        ModelState.Clear();
        if (!TryValidateModel(model))
        {
            return View(model);
        }

        try
        {
            var updated = await api.UpdateProfileAsync(new UpdateProfileBody(
                model.FullName,
                model.Email,
                model.IsOrganiser ? model.OrganisationName : null,
                model.IsOrganiser ? model.ContactNumber : null,
                model.IsOrganiser ? null : model.DateOfBirth), ct);

            HttpContext.Session.UpdateProfile(updated);
            FlashSuccess("Your profile has been updated.");
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            AddApiError(ex);
            return View(model);
        }
    }

    /// <summary>Sends the picture to the API, which stores it in Azure Blob Storage and saves the URL on the profile.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireRole(RaceDayValues.Participant)]
    public async Task<IActionResult> UploadPicture(IFormFile? picture, CancellationToken ct)
    {
        if (picture is null || picture.Length == 0)
        {
            FlashError("Choose an image to upload.");
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var updated = await api.UploadProfilePictureAsync(picture, ct);
            HttpContext.Session.UpdateProfile(updated);
            FlashSuccess("Your profile picture has been updated.");
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            FlashError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }
}
