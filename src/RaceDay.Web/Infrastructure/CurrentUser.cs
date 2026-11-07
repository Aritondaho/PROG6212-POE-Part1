using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Infrastructure;

/// <summary>The signed-in user as stored in the MVC session. Views and controllers read it through <see cref="CurrentUserExtensions"/>.</summary>
public sealed record CurrentUser(int UserId, string Role, string FullName, string? ProfilePictureUrl)
{
    public bool IsOrganiser => Role == RaceDayValues.Organiser;

    public bool IsParticipant => Role == RaceDayValues.Participant;

    /// <summary>First letters of the first two names, shown when there is no profile picture.</summary>
    public string Initials => string.Concat(FullName
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(part => char.ToUpperInvariant(part[0])));
}

public static class CurrentUserExtensions
{
    /// <summary>Returns the signed-in user, or null for a visitor who has not signed in.</summary>
    public static CurrentUser? GetCurrentUser(this HttpContext context)
    {
        var session = context.Session;
        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);
        if (userId is null || string.IsNullOrEmpty(role))
        {
            return null;
        }

        return new CurrentUser(
            userId.Value,
            role,
            session.GetString(SessionKeys.FullName) ?? string.Empty,
            session.GetString(SessionKeys.ProfilePictureUrl));
    }

    /// <summary>Stores the profile returned by the API's login endpoint. The API cookie is already stored by the handler.</summary>
    public static void SignIn(this ISession session, UserProfile profile)
    {
        session.SetInt32(SessionKeys.UserId, profile.UserID);
        session.SetString(SessionKeys.Role, profile.Role);
        session.UpdateProfile(profile);
    }

    /// <summary>Refreshes the name and picture shown in the navigation bar after a profile change.</summary>
    public static void UpdateProfile(this ISession session, UserProfile profile)
    {
        session.SetString(SessionKeys.FullName, profile.FullName);
        if (string.IsNullOrEmpty(profile.ProfilePictureUrl))
        {
            session.Remove(SessionKeys.ProfilePictureUrl);
        }
        else
        {
            session.SetString(SessionKeys.ProfilePictureUrl, profile.ProfilePictureUrl);
        }
    }
}
