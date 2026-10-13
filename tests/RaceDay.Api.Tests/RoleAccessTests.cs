using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests;

/// <summary>
/// Every role-restricted endpoint must reject the wrong role with 403 and an anonymous caller with 401.
/// These tests call the endpoints directly, so the checks come from the API and not from the UI.
/// </summary>
public class RoleAccessTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);

        var response = await participant.PostAsJsonAsync("/api/events", ApiHelpers.SampleEvent("Participant Event"));

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Participant_CannotDeleteEvent_Returns403()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var evt = await ApiHelpers.CreateEventAsync(organiser, "Organiser Only Delete");
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);

        var response = await participant.DeleteAsync($"/api/events/{evt.EventID}");

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory_Returns403()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var evt = await ApiHelpers.CreateEventAsync(organiser, "Category Guard");
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);

        var response = await participant.PostAsJsonAsync($"/api/events/{evt.EventID}/categories", new CategoryRequest { Name = "Sneaky", MinAge = 18 });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments_Returns403()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var evt = await ApiHelpers.CreateEventAsync(organiser, "Enrolment Privacy");
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);

        var response = await participant.GetAsync($"/api/events/{evt.EventID}/enrolments");

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Participant_CannotRecordResults_Returns403()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var evt = await ApiHelpers.CreateEventAsync(organiser, "Results Guard");
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);

        var response = await participant.PostAsJsonAsync($"/api/events/{evt.EventID}/results", new CreateResultRequest
        {
            EnrolmentId = 1,
            FinishTime = TimeSpan.FromMinutes(50),
            FinishPosition = 1,
        });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Organiser_CannotEnrolInAnEvent_Returns403()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var evt = await ApiHelpers.CreateEventAsync(organiser, "Organiser Cannot Enrol");
        var category = await ApiHelpers.CreateCategoryAsync(organiser, evt.EventID);

        var response = await organiser.PostAsJsonAsync($"/api/events/{evt.EventID}/enrol", new EnrolRequest { CategoryId = category.CategoryID });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Organiser_CannotReadParticipantResultHistory_Returns403()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);

        var response = await organiser.GetAsync("/api/results/me");

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AnonymousUser_CannotCreateEvent_Returns401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/events", ApiHelpers.SampleEvent("Anonymous"));

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnonymousUser_CannotListEnrolments_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/enrolments");

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnonymousUser_CannotUpdateProfile_Returns401()
    {
        var response = await factory.CreateClient().PutAsJsonAsync("/api/users/profile", new UpdateProfileRequest
        {
            FullName = "Nobody",
            Email = ApiHelpers.UniqueEmail("nobody"),
            OrganisationName = "None",
            ContactNumber = "0",
        });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnonymousUser_CannotRecordResults_Returns401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/events/1/results", new CreateResultRequest
        {
            EnrolmentId = 1,
            FinishTime = TimeSpan.FromMinutes(50),
            FinishPosition = 1,
        });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OrganiserProfile_IsNotTheParticipantProfile()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);

        var profile = await organiser.GetFromJsonAsync<UserProfileResponse>("/api/users/profile");

        Assert.Equal(UserRoles.Organiser, profile!.Role);
        Assert.Null(profile.DateOfBirth);
    }
}
