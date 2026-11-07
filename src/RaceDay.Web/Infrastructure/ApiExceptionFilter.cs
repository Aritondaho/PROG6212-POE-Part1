using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Web.Services;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Turns API failures that a controller did not handle into friendly pages instead of raw errors:
/// <list type="bullet">
/// <item><description>401 (API session expired): clear the MVC session and go to the login page.</description></item>
/// <item><description>403: the access denied page. 404: the "not found" page.</description></item>
/// <item><description>API unreachable or timed out: the "service unavailable" page.</description></item>
/// <item><description>Any other API error: the general error page with the API's message.</description></item>
/// </list>
/// </summary>
public class ApiExceptionFilter(ITempDataDictionaryFactory tempDataFactory, ILogger<ApiExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var http = context.HttpContext;

        switch (context.Exception)
        {
            case ApiUnauthorizedException:
                http.Session.Clear();
                tempDataFactory.GetTempData(http)[FlashMessages.Info] = "Your session has ended. Please sign in again.";
                var returnUrl = HttpMethods.IsGet(http.Request.Method) ? http.Request.Path + http.Request.QueryString : null;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                break;

            case ApiException { StatusCode: HttpStatusCode.Forbidden }:
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                break;

            case ApiException { StatusCode: HttpStatusCode.NotFound }:
                context.Result = ErrorView(context, "NotFound", StatusCodes.Status404NotFound, null);
                break;

            case ApiException apiError:
                logger.LogWarning(apiError, "Unhandled API error {StatusCode} on {Path}", apiError.StatusCode, http.Request.Path);
                context.Result = ErrorView(context, "Error", StatusCodes.Status400BadRequest, apiError.Message);
                break;

            case HttpRequestException or TaskCanceledException when !http.RequestAborted.IsCancellationRequested:
                logger.LogError(context.Exception, "The RaceDay API could not be reached");
                context.Result = ErrorView(context, "ServiceUnavailable", StatusCodes.Status503ServiceUnavailable, null);
                break;

            default:
                return;
        }

        context.ExceptionHandled = true;
    }

    private static ViewResult ErrorView(ExceptionContext context, string viewName, int statusCode, string? message)
    {
        var modelMetadata = context.HttpContext.RequestServices
            .GetRequiredService<Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider>();

        return new ViewResult
        {
            ViewName = $"~/Views/Shared/{viewName}.cshtml",
            StatusCode = statusCode,
            ViewData = new ViewDataDictionary(modelMetadata, context.ModelState) { ["Message"] = message },
        };
    }
}
