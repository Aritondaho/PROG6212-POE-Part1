using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests;

/// <summary>Organisers manage their own events. Anyone can browse them.</summary>
public class EventManagementTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201_AndItIsPubliclyListed()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);

        var created = await ApiHelpers.CreateEventAsync(organiser, "Public Listing Run");

        var listing = await factory.CreateClient().GetFromJsonAsync<List<EventResponse>>("/api/events");
        Assert.Contains(listing!, e => e.EventID == created.EventID && e.Name == "Public Listing Run");
    }

    [Fact]
    public async Task CreateEvent_Returns201_WithCreatedLocationAndOrganiserName()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);

        var response = await organiser.PostAsJsonAsync("/api/events", ApiHelpers.SampleEvent("Location Check"));

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Created);
        Assert.NotNull(response.Headers.Location);
        var evt = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("Test Events", evt!.OrganisationName);
        Assert.Equal(EventTypes.Run, evt.EventType);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent_Returns200()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var created = await ApiHelpers.CreateEventAsync(organiser, "Before Update");

        var response = await organiser.PutAsJsonAsync($"/api/events/{created.EventID}", ApiHelpers.SampleEvent("After Update", EventTypes.Walk));

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("After Update", updated!.Name);
        Assert.Equal(EventTypes.Walk, updated.EventType);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_Returns204_AndEventIsGone()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var created = await ApiHelpers.CreateEventAsync(organiser, "To Be Deleted");

        var delete = await organiser.DeleteAsync($"/api/events/{created.EventID}");
        var lookup = await factory.CreateClient().GetAsync($"/api/events/{created.EventID}");

        await ApiHelpers.AssertStatusAsync(delete, HttpStatusCode.NoContent);
        await ApiHelpers.AssertStatusAsync(lookup, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_Returns403()
    {
        var owner = await ApiHelpers.SignedInOrganiserAsync(factory);
        var intruder = await ApiHelpers.SignedInOrganiserAsync(factory);
        var created = await ApiHelpers.CreateEventAsync(owner, "Owned By First Organiser");

        var response = await intruder.PutAsJsonAsync($"/api/events/{created.EventID}", ApiHelpers.SampleEvent("Hijacked"));

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent_Returns403()
    {
        var owner = await ApiHelpers.SignedInOrganiserAsync(factory);
        var intruder = await ApiHelpers.SignedInOrganiserAsync(factory);
        var created = await ApiHelpers.CreateEventAsync(owner, "Protected Event");

        var response = await intruder.DeleteAsync($"/api/events/{created.EventID}");

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AnonymousUser_CanListEvents_Returns200()
    {
        var response = await factory.CreateClient().GetAsync("/api/events");

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListEvents_FilteredByType_ReturnsOnlyThatType()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var cycle = await organiser.PostAsJsonAsync("/api/events", ApiHelpers.SampleEvent("Filter Cycle", EventTypes.Cycle));
        cycle.EnsureSuccessStatusCode();
        var createdCycle = await cycle.Content.ReadFromJsonAsync<EventResponse>();

        var listing = await factory.CreateClient().GetFromJsonAsync<List<EventResponse>>("/api/events?type=Cycle");

        Assert.NotEmpty(listing!);
        Assert.All(listing!, e => Assert.Equal(EventTypes.Cycle, e.EventType));
        Assert.Contains(listing!, e => e.EventID == createdCycle!.EventID);
    }

    [Fact]
    public async Task ListEvents_WithUnknownType_Returns400()
    {
        var response = await factory.CreateClient().GetAsync("/api/events?type=Swim");

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEvent_WithUnknownEventType_Returns400()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);

        var response = await organiser.PostAsJsonAsync("/api/events", ApiHelpers.SampleEvent("Bad Type", "Swim"));

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetEvent_WithUnknownId_Returns404()
    {
        var response = await factory.CreateClient().GetAsync("/api/events/999999");

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UploadBanner_WithPngImage_StoresUrlOnTheEvent()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var created = await ApiHelpers.CreateEventAsync(organiser, "Banner Event");

        using var form = new MultipartFormDataContent();
        var image = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(image, "file", "banner.png");

        var response = await organiser.PostAsync($"/api/events/{created.EventID}/upload-banner", form);

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.OK);
        var upload = await response.Content.ReadFromJsonAsync<BannerUploadResponse>();
        Assert.StartsWith("/uploads/banners/", upload!.ImageUrl);
        Assert.EndsWith(".png", upload.ImageUrl);

        var refreshed = await factory.CreateClient().GetFromJsonAsync<EventResponse>($"/api/events/{created.EventID}");
        Assert.Equal(upload.ImageUrl, refreshed!.BannerImageUrl);
    }

    [Fact]
    public async Task UploadBanner_WithNonImageFile_Returns400()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var created = await ApiHelpers.CreateEventAsync(organiser, "Not An Image");

        using var form = new MultipartFormDataContent();
        var text = new ByteArrayContent("hello"u8.ToArray());
        text.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(text, "file", "notes.txt");

        var response = await organiser.PostAsync($"/api/events/{created.EventID}/upload-banner", form);

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }
}
