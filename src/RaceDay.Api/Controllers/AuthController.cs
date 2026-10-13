using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>Registration and login. Login stores the user's ID and role in the server-side session.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(RaceDayDbContext db, IPasswordHasher<User> passwordHasher) : RaceDayControllerBase
{
    /// <summary>Creates an Organiser or Participant account.</summary>
    /// <remarks>
    /// Organisers must send organisationName and contactNumber. Participants must send dateOfBirth,
    /// which must be in the past. The password is hashed with PBKDF2 before it is stored.
    /// Registering does not sign the user in. Call POST /api/auth/login next.
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserProfileResponse>> Register([FromBody] RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var role = request.Role.Trim();

        if (role is not (UserRoles.Organiser or UserRoles.Participant))
        {
            return Problem(detail: "Role must be either Organiser or Participant.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (role == UserRoles.Organiser && (string.IsNullOrWhiteSpace(request.OrganisationName) || string.IsNullOrWhiteSpace(request.ContactNumber)))
        {
            return Problem(detail: "organisationName and contactNumber are required for Organisers.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (role == UserRoles.Participant)
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

        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return Problem(detail: "An account with this email already exists.", statusCode: StatusCodes.Status400BadRequest);
        }

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            Role = role,
            CreatedAt = DateTime.Now,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        if (role == UserRoles.Organiser)
        {
            user.Organiser = new Organiser
            {
                OrganisationName = request.OrganisationName!.Trim(),
                ContactNumber = request.ContactNumber!.Trim(),
            };
        }
        else
        {
            user.Participant = new Participant { DateOfBirth = request.DateOfBirth!.Value };
        }

        // One SaveChanges writes the user and the matching profile row in a single transaction.
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, UserProfileResponse.FromUser(user));
    }

    /// <summary>Signs in with email and password and starts a session.</summary>
    /// <remarks>
    /// On success the server creates a session that stores the user's ID and role, and the response sets the
    /// RaceDay.Session cookie. Swagger UI and browsers send this cookie automatically on later requests.
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await db.Users
            .Include(u => u.Organiser)
            .Include(u => u.Participant)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Problem(detail: "Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);
        }

        // Clear any existing session before signing in, so an old session cannot carry over to the new login.
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserID);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);

        return Ok(UserProfileResponse.FromUser(user));
    }

    /// <summary>Ends the current session. Later requests that reuse the old cookie get 401.</summary>
    [HttpPost("logout")]
    [SessionAuthorize]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new MessageResponse("Signed out."));
    }
}
