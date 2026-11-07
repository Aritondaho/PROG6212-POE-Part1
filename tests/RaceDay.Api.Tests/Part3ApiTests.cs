using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests;

/// <summary>Endpoints added in Part 3 for the MVC application: the organiser dashboard and profile pictures.</summary>
public class Part3ApiTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task MyEvents_ReturnsOnlyTheOrganisersOwnEvents_WithEnrolmentCounts()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var otherOrganiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var mine = await ApiHelpers.CreateEventAsync(organiser, "Dashboard Run");
        var theirs = await ApiHelpers.CreateEventAsync(otherOrganiser, "Someone Else's Run");
        var category = await ApiHelpers.CreateCategoryAsync(organiser, mine.EventID);

        var participant = await ApiHelpers.SignedInParticipantAsync(factory);
        var enrol = await participant.PostAsJsonAsync($"/api/events/{mine.EventID}/enrol", new EnrolRequest { CategoryId = category.CategoryID });
        await ApiHelpers.AssertStatusAsync(enrol, HttpStatusCode.Created);

        var summaries = await organiser.GetFromJsonAsync<List<OrganiserEventSummaryResponse>>("/api/events/mine");

        var row = Assert.Single(summaries!);
        Assert.Equal(mine.EventID, row.Event.EventID);
        Assert.Equal("Test Events", row.Event.OrganisationName);
        Assert.Equal(1, row.EnrolmentCount);
        Assert.Equal(0, row.ConfirmedCount);
        Assert.Equal(1, row.CategoryCount);
        Assert.DoesNotContain(summaries!, s => s.Event.EventID == theirs.EventID);
    }

    [Fact]
    public async Task MyEvents_AsParticipant_Returns403()
    {
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);

        var response = await participant.GetAsync("/api/events/mine");

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MyEvents_WithoutSession_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/events/mine");

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadProfilePicture_AsParticipant_SavesUrlOnTheProfile()
    {
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);

        using var form = PngForm();
        var response = await participant.PostAsync("/api/users/profile/picture", form);

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.StartsWith("/uploads/profile-pictures/", profile!.ProfilePictureUrl);

        var reloaded = await participant.GetFromJsonAsync<UserProfileResponse>("/api/users/profile");
        Assert.Equal(profile.ProfilePictureUrl, reloaded!.ProfilePictureUrl);
    }

    [Fact]
    public async Task UploadProfilePicture_WithNonImageFile_Returns400()
    {
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);

        using var form = new MultipartFormDataContent();
        var text = new ByteArrayContent("not a picture"u8.ToArray());
        text.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(text, "file", "notes.txt");

        var response = await participant.PostAsync("/api/users/profile/picture", form);

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadProfilePicture_AsOrganiser_Returns403()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);

        using var form = PngForm();
        var response = await organiser.PostAsync("/api/users/profile/picture", form);

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    private static MultipartFormDataContent PngForm()
    {
        var form = new MultipartFormDataContent();
        var image = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(image, "file", "me.png");
        return form;
    }
}
