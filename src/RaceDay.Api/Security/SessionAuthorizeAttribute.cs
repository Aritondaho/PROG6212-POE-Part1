using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Security;

/// <summary>
/// Protects an endpoint with the server-side session.
/// <list type="bullet">
/// <item><description><c>[SessionAuthorize]</c>: any signed-in user may call the endpoint.</description></item>
/// <item><description><c>[SessionAuthorize("Organiser")]</c>: only sessions with that role may call it.</description></item>
/// </list>
/// No session returns 401. A session whose role is not allowed returns 403.
/// The role is read from the server-side session, never from the request, so a client cannot change its own role.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class SessionAuthorizeAttribute(params string[] allowedRoles) : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        await session.LoadAsync(context.HttpContext.RequestAborted);

        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);

        if (userId is null || role is null)
        {
            context.Result = Reject(StatusCodes.Status401Unauthorized, "Sign in to use this endpoint.");
            return;
        }

        if (allowedRoles.Length > 0 && !allowedRoles.Contains(role))
        {
            context.Result = Reject(
                StatusCodes.Status403Forbidden,
                $"This endpoint is only available to {string.Join(" or ", allowedRoles)} accounts.");
        }
    }

    private static ObjectResult Reject(int statusCode, string detail) =>
        new(new ProblemDetails { Title = "Access denied", Status = statusCode, Detail = detail })
        {
            StatusCode = statusCode,
        };
}
