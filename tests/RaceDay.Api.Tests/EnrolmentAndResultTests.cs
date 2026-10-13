using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests;

/// <summary>Participants enrol in events by category, organisers confirm enrolments, and results are recorded and read back.</summary>
public class EnrolmentAndResultTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task Participant_CanEnrolInACategory_Returns201_AndRecordsTheLinkInTheDatabase()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var evt = await ApiHelpers.CreateEventAsync(organiser, "Enrol Me");
        var category = await ApiHelpers.CreateCategoryAsync(organiser, evt.EventID, "Senior");
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);

        var response = await participant.PostAsJsonAsync($"/api/events/{evt.EventID}/enrol", new EnrolRequest { CategoryId = category.CategoryID });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Created);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.Equal(EnrolmentStatuses.Pending, enrolment!.Status);
        Assert.Equal(evt.EventID, enrolment.EventID);
        Assert.Equal(category.CategoryID, enrolment.CategoryID);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var stored = await db.Enrolments.SingleAsync(en => en.EnrolmentID == enrolment.EnrolmentID);
        Assert.Equal(enrolment.ParticipantID, stored.ParticipantID);
        Assert.Equal(evt.EventID, stored.EventID);
        Assert.Equal(category.CategoryID, stored.CategoryID);
    }

    [Fact]
    public async Task Participant_EnrolsTwiceInTheSameEvent_Returns400()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var evt = await ApiHelpers.CreateEventAsync(organiser, "Once Only");
        var category = await ApiHelpers.CreateCategoryAsync(organiser, evt.EventID);
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);
        var request = new EnrolRequest { CategoryId = category.CategoryID };

        (await participant.PostAsJsonAsync($"/api/events/{evt.EventID}/enrol", request)).EnsureSuccessStatusCode();
        var second = await participant.PostAsJsonAsync($"/api/events/{evt.EventID}/enrol", request);

        await ApiHelpers.AssertStatusAsync(second, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Participant_UsesACategoryFromAnotherEvent_Returns400()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var eventA = await ApiHelpers.CreateEventAsync(organiser, "Event A");
        var eventB = await ApiHelpers.CreateEventAsync(organiser, "Event B");
        var categoryOfB = await ApiHelpers.CreateCategoryAsync(organiser, eventB.EventID);
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventA.EventID}/enrol", new EnrolRequest { CategoryId = categoryOfB.CategoryID });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Participant_CanListTheirOwnEnrolments()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var evt = await ApiHelpers.CreateEventAsync(organiser, "My Enrolments");
        var category = await ApiHelpers.CreateCategoryAsync(organiser, evt.EventID);
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);
        (await participant.PostAsJsonAsync($"/api/events/{evt.EventID}/enrol", new EnrolRequest { CategoryId = category.CategoryID })).EnsureSuccessStatusCode();

        var mine = await participant.GetFromJsonAsync<List<EnrolmentResponse>>("/api/enrolments");

        Assert.Single(mine!);
        Assert.Equal(evt.EventID, mine![0].EventID);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForTheirEvent()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var evt = await ApiHelpers.CreateEventAsync(organiser, "Organiser View");
        var category = await ApiHelpers.CreateCategoryAsync(organiser, evt.EventID);
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);
        (await participant.PostAsJsonAsync($"/api/events/{evt.EventID}/enrol", new EnrolRequest { CategoryId = category.CategoryID })).EnsureSuccessStatusCode();

        var enrolments = await organiser.GetFromJsonAsync<List<EnrolmentResponse>>($"/api/events/{evt.EventID}/enrolments");

        Assert.Single(enrolments!);
        Assert.Equal("Test Participant", enrolments![0].ParticipantName);
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsForAnotherOrganisersEvent_Returns403()
    {
        var owner = await ApiHelpers.SignedInOrganiserAsync(factory);
        var evt = await ApiHelpers.CreateEventAsync(owner, "Owner Only Enrolments");
        var other = await ApiHelpers.SignedInOrganiserAsync(factory);

        var response = await other.GetAsync($"/api/events/{evt.EventID}/enrolments");

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Organiser_CanConfirmAnEnrolment_Returns200()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var evt = await ApiHelpers.CreateEventAsync(organiser, "Confirm Me");
        var category = await ApiHelpers.CreateCategoryAsync(organiser, evt.EventID);
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);
        var enrolment = await (await participant.PostAsJsonAsync($"/api/events/{evt.EventID}/enrol", new EnrolRequest { CategoryId = category.CategoryID }))
            .Content.ReadFromJsonAsync<EnrolmentResponse>();

        var response = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolment!.EnrolmentID}/status", new UpdateEnrolmentStatusRequest { Status = EnrolmentStatuses.Confirmed });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.Equal(EnrolmentStatuses.Confirmed, updated!.Status);
    }

    [Fact]
    public async Task Organiser_RecordsAResult_AndParticipantSeesPositionOutOfFinishers()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var evt = await ApiHelpers.CreateEventAsync(organiser, "Results Day");
        var category = await ApiHelpers.CreateCategoryAsync(organiser, evt.EventID);

        var winner = await ApiHelpers.SignedInParticipantAsync(factory);
        var winnerEnrolment = await (await winner.PostAsJsonAsync($"/api/events/{evt.EventID}/enrol", new EnrolRequest { CategoryId = category.CategoryID }))
            .Content.ReadFromJsonAsync<EnrolmentResponse>();

        var second = await ApiHelpers.SignedInParticipantAsync(factory);
        var secondEnrolment = await (await second.PostAsJsonAsync($"/api/events/{evt.EventID}/enrol", new EnrolRequest { CategoryId = category.CategoryID }))
            .Content.ReadFromJsonAsync<EnrolmentResponse>();

        var record = await organiser.PostAsJsonAsync($"/api/events/{evt.EventID}/results", new CreateResultRequest
        {
            EnrolmentId = winnerEnrolment!.EnrolmentID,
            FinishTime = new TimeSpan(0, 0, 41, 5),
            FinishPosition = 1,
        });
        await ApiHelpers.AssertStatusAsync(record, HttpStatusCode.Created);

        (await organiser.PostAsJsonAsync($"/api/events/{evt.EventID}/results", new CreateResultRequest
        {
            EnrolmentId = secondEnrolment!.EnrolmentID,
            FinishTime = new TimeSpan(0, 0, 44, 10),
            FinishPosition = 2,
        })).EnsureSuccessStatusCode();

        var history = await winner.GetFromJsonAsync<List<ResultResponse>>("/api/results/me");

        var mine = Assert.Single(history!);
        Assert.Equal(new TimeSpan(0, 0, 41, 5), mine.FinishTime);
        Assert.Equal(1, mine.FinishPosition);
        Assert.Equal(2, mine.TotalFinishers);
        Assert.Equal("Results Day", mine.EventName);
        Assert.Equal(category.Name, mine.CategoryName);
    }

    [Fact]
    public async Task Organiser_RecordingASecondResultForTheSameEnrolment_Returns409()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var evt = await ApiHelpers.CreateEventAsync(organiser, "Duplicate Result");
        var category = await ApiHelpers.CreateCategoryAsync(organiser, evt.EventID);
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);
        var enrolment = await (await participant.PostAsJsonAsync($"/api/events/{evt.EventID}/enrol", new EnrolRequest { CategoryId = category.CategoryID }))
            .Content.ReadFromJsonAsync<EnrolmentResponse>();
        var request = new CreateResultRequest { EnrolmentId = enrolment!.EnrolmentID, FinishTime = TimeSpan.FromMinutes(55), FinishPosition = 3 };

        (await organiser.PostAsJsonAsync($"/api/events/{evt.EventID}/results", request)).EnsureSuccessStatusCode();
        var second = await organiser.PostAsJsonAsync($"/api/events/{evt.EventID}/results", request);

        await ApiHelpers.AssertStatusAsync(second, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Organiser_RecordingAResultForAnEnrolmentFromAnotherEvent_Returns400()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var eventA = await ApiHelpers.CreateEventAsync(organiser, "Result Event A");
        var eventB = await ApiHelpers.CreateEventAsync(organiser, "Result Event B");
        var categoryB = await ApiHelpers.CreateCategoryAsync(organiser, eventB.EventID);
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);
        var enrolmentInB = await (await participant.PostAsJsonAsync($"/api/events/{eventB.EventID}/enrol", new EnrolRequest { CategoryId = categoryB.CategoryID }))
            .Content.ReadFromJsonAsync<EnrolmentResponse>();

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventA.EventID}/results", new CreateResultRequest
        {
            EnrolmentId = enrolmentInB!.EnrolmentID,
            FinishTime = TimeSpan.FromMinutes(60),
            FinishPosition = 1,
        });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Organiser_RecordingAFinishTimeOfZero_Returns400()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var evt = await ApiHelpers.CreateEventAsync(organiser, "Zero Time");

        var response = await organiser.PostAsJsonAsync($"/api/events/{evt.EventID}/results", new CreateResultRequest
        {
            EnrolmentId = 1,
            FinishTime = TimeSpan.Zero,
            FinishPosition = 1,
        });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteEvent_WithEnrolments_Returns409_AndKeepsTheEnrolment()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var evt = await ApiHelpers.CreateEventAsync(organiser, "Protected By Enrolments");
        var category = await ApiHelpers.CreateCategoryAsync(organiser, evt.EventID);
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);
        (await participant.PostAsJsonAsync($"/api/events/{evt.EventID}/enrol", new EnrolRequest { CategoryId = category.CategoryID })).EnsureSuccessStatusCode();

        var response = await organiser.DeleteAsync($"/api/events/{evt.EventID}");

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Conflict);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        Assert.True(await db.Enrolments.AnyAsync(en => en.EventID == evt.EventID));
    }

    [Fact]
    public async Task DeleteCategory_WithEnrolments_Returns409()
    {
        var organiser = await ApiHelpers.SignedInOrganiserAsync(factory);
        var evt = await ApiHelpers.CreateEventAsync(organiser, "Category In Use");
        var category = await ApiHelpers.CreateCategoryAsync(organiser, evt.EventID);
        var participant = await ApiHelpers.SignedInParticipantAsync(factory);
        (await participant.PostAsJsonAsync($"/api/events/{evt.EventID}/enrol", new EnrolRequest { CategoryId = category.CategoryID })).EnsureSuccessStatusCode();

        var response = await organiser.DeleteAsync($"/api/categories/{category.CategoryID}");

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Conflict);
    }
}
