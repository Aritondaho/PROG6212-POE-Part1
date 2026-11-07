using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

/// <summary>
/// Typed client for every RaceDay API endpoint the MVC application uses. This is the only way the MVC
/// project reads or changes data. The signed-in user's API session travels with each call through
/// <see cref="ApiSessionCookieHandler"/>, so the API still decides who may do what.
/// </summary>
public class RaceDayApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ---- Authentication -------------------------------------------------------------------------

    public Task<UserProfile> RegisterAsync(RegisterBody body, CancellationToken ct = default) =>
        SendAsync<UserProfile>(HttpMethod.Post, "api/auth/register", body, ct);

    public Task<UserProfile> LoginAsync(LoginBody body, CancellationToken ct = default) =>
        SendAsync<UserProfile>(HttpMethod.Post, "api/auth/login", body, ct);

    public Task LogoutAsync(CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, "api/auth/logout", null, ct);

    // ---- Profile --------------------------------------------------------------------------------

    public Task<UserProfile> GetProfileAsync(CancellationToken ct = default) =>
        SendAsync<UserProfile>(HttpMethod.Get, "api/users/profile", null, ct);

    public Task<UserProfile> UpdateProfileAsync(UpdateProfileBody body, CancellationToken ct = default) =>
        SendAsync<UserProfile>(HttpMethod.Put, "api/users/profile", body, ct);

    public Task<UserProfile> UploadProfilePictureAsync(IFormFile file, CancellationToken ct = default) =>
        UploadAsync<UserProfile>("api/users/profile/picture", file, ct);

    // ---- Events ---------------------------------------------------------------------------------

    public Task<List<EventItem>> GetEventsAsync(string? type, DateOnly? date, string? location, CancellationToken ct = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(type)) query.Add($"type={Uri.EscapeDataString(type)}");
        if (date is not null) query.Add($"date={date.Value:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(location)) query.Add($"location={Uri.EscapeDataString(location)}");

        var url = query.Count == 0 ? "api/events" : $"api/events?{string.Join('&', query)}";
        return SendAsync<List<EventItem>>(HttpMethod.Get, url, null, ct);
    }

    /// <summary>Returns the event, or null when the API says it does not exist.</summary>
    public async Task<EventItem?> GetEventAsync(int id, CancellationToken ct = default)
    {
        try
        {
            return await SendAsync<EventItem>(HttpMethod.Get, $"api/events/{id}", null, ct);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task<List<OrganiserEventSummary>> GetMyEventsAsync(CancellationToken ct = default) =>
        SendAsync<List<OrganiserEventSummary>>(HttpMethod.Get, "api/events/mine", null, ct);

    public Task<EventItem> CreateEventAsync(EventBody body, CancellationToken ct = default) =>
        SendAsync<EventItem>(HttpMethod.Post, "api/events", body, ct);

    public Task<EventItem> UpdateEventAsync(int id, EventBody body, CancellationToken ct = default) =>
        SendAsync<EventItem>(HttpMethod.Put, $"api/events/{id}", body, ct);

    public Task DeleteEventAsync(int id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"api/events/{id}", null, ct);

    /// <summary>Sends the banner file to the API, which stores it in Azure Blob Storage and saves the URL on the event.</summary>
    public Task<BannerUpload> UploadBannerAsync(int eventId, IFormFile file, CancellationToken ct = default) =>
        UploadAsync<BannerUpload>($"api/events/{eventId}/upload-banner", file, ct);

    // ---- Categories -----------------------------------------------------------------------------

    public Task<List<CategoryItem>> GetCategoriesAsync(int eventId, CancellationToken ct = default) =>
        SendAsync<List<CategoryItem>>(HttpMethod.Get, $"api/events/{eventId}/categories", null, ct);

    public Task<CategoryItem> CreateCategoryAsync(int eventId, CategoryBody body, CancellationToken ct = default) =>
        SendAsync<CategoryItem>(HttpMethod.Post, $"api/events/{eventId}/categories", body, ct);

    public Task<CategoryItem> UpdateCategoryAsync(int id, CategoryBody body, CancellationToken ct = default) =>
        SendAsync<CategoryItem>(HttpMethod.Put, $"api/categories/{id}", body, ct);

    public Task DeleteCategoryAsync(int id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"api/categories/{id}", null, ct);

    // ---- Enrolments -----------------------------------------------------------------------------

    /// <summary>Participants get their own enrolments. Organisers get the enrolments for their events.</summary>
    public Task<List<EnrolmentItem>> GetEnrolmentsAsync(CancellationToken ct = default) =>
        SendAsync<List<EnrolmentItem>>(HttpMethod.Get, "api/enrolments", null, ct);

    public Task<EnrolmentItem> EnrolAsync(int eventId, int categoryId, CancellationToken ct = default) =>
        SendAsync<EnrolmentItem>(HttpMethod.Post, $"api/events/{eventId}/enrol", new EnrolBody(categoryId), ct);

    public Task<List<EnrolmentItem>> GetEventEnrolmentsAsync(int eventId, CancellationToken ct = default) =>
        SendAsync<List<EnrolmentItem>>(HttpMethod.Get, $"api/events/{eventId}/enrolments", null, ct);

    public Task<EnrolmentItem> UpdateEnrolmentStatusAsync(int enrolmentId, string status, CancellationToken ct = default) =>
        SendAsync<EnrolmentItem>(HttpMethod.Put, $"api/enrolments/{enrolmentId}/status", new EnrolmentStatusBody(status), ct);

    // ---- Results --------------------------------------------------------------------------------

    public Task<List<ResultItem>> GetMyResultsAsync(CancellationToken ct = default) =>
        SendAsync<List<ResultItem>>(HttpMethod.Get, "api/results/me", null, ct);

    public Task<List<ResultItem>> GetEventResultsAsync(int eventId, CancellationToken ct = default) =>
        SendAsync<List<ResultItem>>(HttpMethod.Get, $"api/events/{eventId}/results", null, ct);

    public Task<ResultItem> CreateResultAsync(int eventId, CreateResultBody body, CancellationToken ct = default) =>
        SendAsync<ResultItem>(HttpMethod.Post, $"api/events/{eventId}/results", body, ct);

    public Task<ResultItem> UpdateResultAsync(int resultId, UpdateResultBody body, CancellationToken ct = default) =>
        SendAsync<ResultItem>(HttpMethod.Put, $"api/results/{resultId}", body, ct);

    // ---- Plumbing -------------------------------------------------------------------------------

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var response = await SendRawAsync(method, url, body, ct);
        return (await response.Content.ReadFromJsonAsync<T>(Json, ct))!;
    }

    private async Task SendAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var response = await SendRawAsync(method, url, body, ct);
    }

    private async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        }

        var response = await http.SendAsync(request, ct);
        await ThrowIfFailedAsync(response, ct);
        return response;
    }

    private async Task<T> UploadAsync<T>(string url, IFormFile file, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var content = new StreamContent(stream);
        content.Headers.ContentType = MediaTypeHeaderValue.TryParse(file.ContentType, out var type)
            ? type
            : new MediaTypeHeaderValue("application/octet-stream");
        form.Add(content, "file", Path.GetFileName(file.FileName));

        using var response = await http.PostAsync(url, form, ct);
        await ThrowIfFailedAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(Json, ct))!;
    }

    /// <summary>Turns an error response into an exception that carries the API's own, human-readable explanation.</summary>
    private static async Task ThrowIfFailedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = await ReadProblemMessageAsync(response, ct);
        response.Dispose();

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new ApiUnauthorizedException(message);
        }

        throw new ApiException(response.StatusCode, message);
    }

    private static async Task<string> ReadProblemMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = document.RootElement;

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
            {
                return detail.GetString()!;
            }

            // Model validation failures list their messages under "errors": { "Field": ["message"] }.
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(field => field.Value.EnumerateArray())
                    .Select(item => item.GetString())
                    .Where(text => !string.IsNullOrWhiteSpace(text));
                return string.Join(" ", messages);
            }
        }
        catch (JsonException)
        {
            // Not a ProblemDetails body. Fall through to the generic message.
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Please sign in to continue.",
            HttpStatusCode.Forbidden => "Your account is not allowed to do that.",
            HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
            HttpStatusCode.Conflict => "That change conflicts with existing data.",
            _ => "Something went wrong while talking to the RaceDay service. Please try again.",
        };
    }
}
