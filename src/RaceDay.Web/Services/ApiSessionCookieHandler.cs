using RaceDay.Web.Infrastructure;

namespace RaceDay.Web.Services;

/// <summary>
/// Connects each browser user to their own session on the API.
/// <para>
/// The API keeps the signed-in user's ID and role in a server-side session identified by the "RaceDay.Session" cookie.
/// The browser never talks to the API, so this handler keeps that cookie in the MVC session instead:
/// it sends the stored cookie with every API request, and stores the new one whenever the API sets it (for example on login).
/// </para>
/// </summary>
public class ApiSessionCookieHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    public const string ApiCookieName = "RaceDay.Session";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = httpContextAccessor.HttpContext?.Session;

        var apiCookie = session?.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(apiCookie))
        {
            request.Headers.Remove("Cookie");
            request.Headers.Add("Cookie", $"{ApiCookieName}={apiCookie}");
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (session is not null && response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var header in setCookies)
            {
                if (TryReadCookieValue(header, out var value))
                {
                    session.SetString(SessionKeys.ApiCookie, value);
                }
            }
        }

        return response;
    }

    /// <summary>Reads the value from a header such as "RaceDay.Session=abc; path=/; httponly".</summary>
    public static bool TryReadCookieValue(string setCookieHeader, out string value)
    {
        value = string.Empty;
        var prefix = ApiCookieName + "=";
        if (!setCookieHeader.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var end = setCookieHeader.IndexOf(';');
        value = end < 0 ? setCookieHeader[prefix.Length..] : setCookieHeader[prefix.Length..end];
        return value.Length > 0;
    }
}
