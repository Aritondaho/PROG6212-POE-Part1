using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Protects MVC pages with the session.
/// <list type="bullet">
/// <item><description><c>[RequireRole]</c>: any signed-in user.</description></item>
/// <item><description><c>[RequireRole("Organiser")]</c>: only that role.</description></item>
/// </list>
/// Visitors who are not signed in go to the login page and come back afterwards. A signed-in user with the
/// wrong role goes to the "access denied" page. The API checks the role again on every call, so this filter
/// is about navigation, not the only line of defence.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequireRoleAttribute(params string[] roles) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.GetCurrentUser();

        if (user is null)
        {
            var tempData = context.HttpContext.RequestServices
                .GetRequiredService<ITempDataDictionaryFactory>()
                .GetTempData(context.HttpContext);
            tempData[FlashMessages.Info] = "Please sign in to continue.";

            var request = context.HttpContext.Request;
            var returnUrl = HttpMethods.IsGet(request.Method) ? request.Path + request.QueryString : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (roles.Length > 0 && !roles.Contains(user.Role))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
