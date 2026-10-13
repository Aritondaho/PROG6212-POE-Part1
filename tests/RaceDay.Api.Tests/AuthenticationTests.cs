using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests;

/// <summary>Registration, login, sessions and profile access, for both roles, including the failure paths.</summary>
public class AuthenticationTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task Register_Organiser_Returns201_AndStoresHashedPassword()
    {
        var client = factory.CreateClient();
        var email = ApiHelpers.UniqueEmail("org");

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = ApiHelpers.Password,
            FullName = "Pieter Test",
            Role = UserRoles.Organiser,
            OrganisationName = "Pretoria Sports Events",
            ContactNumber = "+27 82 567 8901",
        });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Created);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.Equal(UserRoles.Organiser, profile!.Role);
        Assert.Equal("Pretoria Sports Events", profile.OrganisationName);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var stored = await db.Users.SingleAsync(u => u.Email == email);

        Assert.NotEqual(ApiHelpers.Password, stored.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>().VerifyHashedPassword(stored, stored.PasswordHash, ApiHelpers.Password));
    }

    [Fact]
    public async Task Register_Participant_Returns201_WithDateOfBirth()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = ApiHelpers.UniqueEmail("par"),
            Password = ApiHelpers.Password,
            FullName = "Amanda Test",
            Role = UserRoles.Participant,
            DateOfBirth = new DateOnly(1991, 12, 5),
        });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Created);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.Equal(UserRoles.Participant, profile!.Role);
        Assert.Equal(new DateOnly(1991, 12, 5), profile.DateOfBirth);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var client = factory.CreateClient();
        var email = ApiHelpers.UniqueEmail("dupe");
        var request = new RegisterRequest
        {
            Email = email,
            Password = ApiHelpers.Password,
            FullName = "First",
            Role = UserRoles.Participant,
            DateOfBirth = new DateOnly(1990, 1, 1),
        };

        (await client.PostAsJsonAsync("/api/auth/register", request)).EnsureSuccessStatusCode();
        var second = await client.PostAsJsonAsync("/api/auth/register", request);

        await ApiHelpers.AssertStatusAsync(second, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_UnknownRole_Returns400()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = ApiHelpers.UniqueEmail("admin"),
            Password = ApiHelpers.Password,
            FullName = "Not Allowed",
            Role = "Administrator",
        });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_OrganiserWithoutOrganisationName_Returns400()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = ApiHelpers.UniqueEmail("noorg"),
            Password = ApiHelpers.Password,
            FullName = "No Organisation",
            Role = UserRoles.Organiser,
            ContactNumber = "+27 11 000 0000",
        });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ParticipantWithoutDateOfBirth_Returns400()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = ApiHelpers.UniqueEmail("nodob"),
            Password = ApiHelpers.Password,
            FullName = "No Birth Date",
            Role = UserRoles.Participant,
        });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_PasswordTooShort_Returns400()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = ApiHelpers.UniqueEmail("short"),
            Password = "short",
            FullName = "Short Password",
            Role = UserRoles.Participant,
            DateOfBirth = new DateOnly(1990, 1, 1),
        });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_Returns200_AndSetsSessionCookie()
    {
        var client = factory.CreateClient();
        var email = ApiHelpers.UniqueEmail("login");
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = ApiHelpers.Password,
            FullName = "Login Tester",
            Role = UserRoles.Participant,
            DateOfBirth = new DateOnly(1990, 1, 1),
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = ApiHelpers.Password });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.OK);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, cookie => cookie.StartsWith("RaceDay.Session=", StringComparison.Ordinal));

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.Equal(email, profile!.Email);
        Assert.Equal(UserRoles.Participant, profile.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = factory.CreateClient();
        var email = ApiHelpers.UniqueEmail("wrongpw");
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = ApiHelpers.Password,
            FullName = "Wrong Password",
            Role = UserRoles.Participant,
            DateOfBirth = new DateOnly(1990, 1, 1),
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "NotTheRightOne!" });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = ApiHelpers.UniqueEmail("ghost"),
            Password = ApiHelpers.Password,
        });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Profile_WithoutSignIn_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/users/profile");

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_EndsTheSession_SoProfileReturns401()
    {
        var client = await ApiHelpers.SignedInOrganiserAsync(factory);
        (await client.GetAsync("/api/users/profile")).EnsureSuccessStatusCode();

        var logout = await client.PostAsync("/api/auth/logout", null);
        var afterLogout = await client.GetAsync("/api/users/profile");

        await ApiHelpers.AssertStatusAsync(logout, HttpStatusCode.OK);
        await ApiHelpers.AssertStatusAsync(afterLogout, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_ChangesOwnDetails_ForAParticipant()
    {
        var client = await ApiHelpers.SignedInParticipantAsync(factory);

        var response = await client.PutAsJsonAsync("/api/users/profile", new UpdateProfileRequest
        {
            FullName = "Updated Name",
            Email = ApiHelpers.UniqueEmail("updated"),
            DateOfBirth = new DateOnly(1992, 2, 2),
        });

        await ApiHelpers.AssertStatusAsync(response, HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.Equal("Updated Name", profile!.FullName);
        Assert.Equal(new DateOnly(1992, 2, 2), profile.DateOfBirth);
    }
}
