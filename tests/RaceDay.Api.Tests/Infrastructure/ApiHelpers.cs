using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests.Infrastructure;

/// <summary>
/// Helpers that drive the API the way a client would: register, sign in, and create data through endpoints.
/// Each client returned by <see cref="RaceDayApiFactory.CreateClient()"/> keeps its own session cookie,
/// so one client is one signed-in user.
/// </summary>
public static class ApiHelpers
{
    public const string Password = "Password123!";

    public static string UniqueEmail(string prefix) => $"{prefix}.{Guid.NewGuid():N}@raceday.test";

    /// <summary>Registers an Organiser through the API and signs that client in.</summary>
    public static async Task<HttpClient> SignedInOrganiserAsync(RaceDayApiFactory factory)
    {
        var client = factory.CreateClient();
        var email = UniqueEmail("organiser");

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = Password,
            FullName = "Test Organiser",
            Role = UserRoles.Organiser,
            OrganisationName = "Test Events",
            ContactNumber = "+27 11 000 0000",
        });
        register.EnsureSuccessStatusCode();

        await LoginAsync(client, email);
        return client;
    }

    /// <summary>Registers a Participant through the API and signs that client in.</summary>
    public static async Task<HttpClient> SignedInParticipantAsync(RaceDayApiFactory factory)
    {
        var client = factory.CreateClient();
        var email = UniqueEmail("participant");

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = Password,
            FullName = "Test Participant",
            Role = UserRoles.Participant,
            DateOfBirth = new DateOnly(1995, 4, 12),
        });
        register.EnsureSuccessStatusCode();

        await LoginAsync(client, email);
        return client;
    }

    public static async Task LoginAsync(HttpClient client, string email, string password = Password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
    }

    /// <summary>A valid event body. Override any field with a <c>with</c> expression.</summary>
    public static EventRequest SampleEvent(string name = "Test Run", string eventType = EventTypes.Run) => new()
    {
        Name = name,
        Description = "Automated test event",
        Date = new DateTime(2027, 3, 14, 7, 0, 0),
        Location = "Pretoria, Gauteng",
        Distance = 10.00m,
        EventType = eventType,
    };

    public static async Task<EventResponse> CreateEventAsync(HttpClient organiser, string name = "Test Run")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", SampleEvent(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventResponse>())!;
    }

    public static async Task<CategoryResponse> CreateCategoryAsync(HttpClient organiser, int eventId, string name = "Open")
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new CategoryRequest
        {
            Name = name,
            MinAge = 18,
            MaxAge = 39,
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }

    /// <summary>Asserts the status code and includes the response body in the failure message, so a failing test explains itself.</summary>
    public static async Task AssertStatusAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Expected {(int)expected} but got {(int)response.StatusCode}. Body: {body}");
        }
    }
}
