namespace RaceDay.Web.Infrastructure;

/// <summary>Keys for the MVC application's own server-side session.</summary>
public static class SessionKeys
{
    /// <summary>Name of the MVC session cookie sent to the browser.</summary>
    public const string CookieName = "RaceDay.Web.Session";

    public const string UserId = "User.Id";
    public const string Role = "User.Role";
    public const string FullName = "User.FullName";
    public const string ProfilePictureUrl = "User.ProfilePictureUrl";

    /// <summary>The API's session cookie value for this user. See <see cref="Services.ApiSessionCookieHandler"/>.</summary>
    public const string ApiCookie = "Api.Cookie";
}
