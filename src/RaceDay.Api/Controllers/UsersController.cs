using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>Profile of the signed-in user. Both roles can read and update only their own profile.</summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
public class UsersController(RaceDayDbContext db) : RaceDayControllerBase
{
    /// <summary>Returns the signed-in user's profile.</summary>
    [HttpGet("profile")]
    [SessionAuthorize]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> GetProfile()
    {
        var user = await LoadCurrentUserAsync();
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(UserProfileResponse.FromUser(user));
    }

    /// <summary>
    /// Updates the signed-in user's profile. Organisers can change organisationName and contactNumber.
    /// Participants can change dateOfBirth. Fields that do not apply to the caller's role are ignored.
    /// </summary>
    [HttpPut("profile")]
    [SessionAuthorize]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = await LoadCurrentUserAsync();
        if (user is null)
        {
            return Unauthorized();
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email && u.UserID != user.UserID))
        {
            return Problem(detail: "Another account already uses this email.", statusCode: StatusCodes.Status400BadRequest);
        }

        // Validate role-specific fields before changing anything.
        if (user.Organiser is not null &&
            (string.IsNullOrWhiteSpace(request.OrganisationName) || string.IsNullOrWhiteSpace(request.ContactNumber)))
        {
            return Problem(detail: "organisationName and contactNumber are required for Organisers.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (user.Participant is not null)
        {
            if (request.DateOfBirth is null)
            {
                return Problem(detail: "dateOfBirth is required for Participants.", statusCode: StatusCodes.Status400BadRequest);
            }

            if (request.DateOfBirth.Value >= DateOnly.FromDateTime(DateTime.Today))
            {
                return Problem(detail: "dateOfBirth must be in the past.", statusCode: StatusCodes.Status400BadRequest);
            }
        }

        user.FullName = request.FullName.Trim();
        user.Email = email;

        if (user.Organiser is not null)
        {
            user.Organiser.OrganisationName = request.OrganisationName!.Trim();
            user.Organiser.ContactNumber = request.ContactNumber!.Trim();
        }

        if (user.Participant is not null)
        {
            user.Participant.DateOfBirth = request.DateOfBirth!.Value;
        }

        await db.SaveChangesAsync();

        return Ok(UserProfileResponse.FromUser(user));
    }

    private Task<User?> LoadCurrentUserAsync() =>
        db.Users
            .Include(u => u.Organiser)
            .Include(u => u.Participant)
            .FirstOrDefaultAsync(u => u.UserID == CurrentUserId);
}
