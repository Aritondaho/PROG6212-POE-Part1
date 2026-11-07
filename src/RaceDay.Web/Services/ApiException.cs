using System.Net;

namespace RaceDay.Web.Services;

/// <summary>
/// The API answered with an error status. <see cref="Exception.Message"/> holds the API's own explanation
/// (the ProblemDetails "detail"), which is written for people and safe to show on the page.
/// </summary>
public class ApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

/// <summary>
/// The API returned 401: the API session has expired or was never created.
/// The MVC app reacts by clearing its own session and sending the user to the login page.
/// </summary>
public class ApiUnauthorizedException(string message) : ApiException(HttpStatusCode.Unauthorized, message);
