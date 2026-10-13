namespace RaceDay.Api.Security;

/// <summary>Keys used for the server-side session that holds the signed-in user's ID and role.</summary>
public static class SessionKeys
{
    /// <summary>Name of the session cookie sent to the browser or Swagger UI.</summary>
    public const string CookieName = "RaceDay.Session";

    public const string UserId = "RaceDay.UserId";

    public const string Role = "RaceDay.Role";
}
