using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// Loads the same sample data as the Part 1 SQL script (2 organisers, 5 participants,
/// 4 events, 15 categories, 10 enrolments and 8 results) into an empty database.
/// Runs only in Development. The Part 1 script used placeholder hashes, so every seeded
/// account here gets a real PasswordHasher hash of <see cref="DemoPassword"/>.
/// </summary>
public static class DbSeeder
{
    public const string DemoPassword = "RaceDay2026!";

    public static async Task SeedAsync(RaceDayDbContext db)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        var hasher = new PasswordHasher<User>();
        var now = DateTime.Now;

        User NewUser(string email, string fullName, string role)
        {
            var user = new User { Email = email, FullName = fullName, Role = role, CreatedAt = now };
            user.PasswordHash = hasher.HashPassword(user, DemoPassword);
            return user;
        }

        // Organisers
        var pretoria = new Organiser
        {
            User = NewUser("race@pretoriasports.co.za", "Pieter van der Merwe", UserRoles.Organiser),
            OrganisationName = "Pretoria Sports Events",
            ContactNumber = "+27 82 567 8901",
        };
        var durban = new Organiser
        {
            User = NewUser("events@durbanactive.com", "Nthabiseng Mokoena", UserRoles.Organiser),
            OrganisationName = "Durban Active Living",
            ContactNumber = "+27 83 654 3210",
        };

        // Participants (index 0 = ParticipantID 1 in the Part 1 script)
        var participants = new[]
        {
            new Participant { User = NewUser("sipho.k@email.com", "Sipho Khumalo", UserRoles.Participant), DateOfBirth = new DateOnly(1994, 8, 10) },
            new Participant { User = NewUser("amanda.w@email.com", "Amanda Williams", UserRoles.Participant), DateOfBirth = new DateOnly(1991, 12, 5) },
            new Participant { User = NewUser("james.p@email.com", "James Patterson", UserRoles.Participant), DateOfBirth = new DateOnly(1987, 3, 19) },
            new Participant { User = NewUser("refiloe.m@email.com", "Refiloe Mabaso", UserRoles.Participant), DateOfBirth = new DateOnly(1993, 6, 25) },
            new Participant { User = NewUser("lindiwe.n@email.com", "Lindiwe Ndlovu", UserRoles.Participant), DateOfBirth = new DateOnly(1998, 11, 30) },
        };

        // Events (index 0 = EventID 1 in the Part 1 script)
        var events = new[]
        {
            new Event { Organiser = pretoria, Name = "Pretoria Marathon", Description = "Flag to flag marathon through the administrative capital", EventDate = new DateTime(2026, 10, 25, 6, 0, 0), Location = "Pretoria, Gauteng", Distance = 42.20m, EventType = EventTypes.Run, CreatedAt = now },
            new Event { Organiser = pretoria, Name = "Voortrekker 5km Fun Run", Description = "Family friendly run in the Voortrekker Monument area", EventDate = new DateTime(2026, 11, 15, 8, 0, 0), Location = "Pretoria, Gauteng", Distance = 5.00m, EventType = EventTypes.Run, CreatedAt = now },
            new Event { Organiser = durban, Name = "Durban Beachfront Walk", Description = "Coastal walk along the golden mile", EventDate = new DateTime(2026, 9, 20, 7, 30, 0), Location = "Durban, KwaZulu-Natal", Distance = 10.00m, EventType = EventTypes.Walk, CreatedAt = now },
            new Event { Organiser = durban, Name = "Sugar Rush Cycling", Description = "Cycle through the sugar cane fields of KZN", EventDate = new DateTime(2026, 10, 5, 6, 30, 0), Location = "Durban, KwaZulu-Natal", Distance = 65.00m, EventType = EventTypes.Cycle, CreatedAt = now },
        };

        // Categories (index 0 = CategoryID 1 in the Part 1 script)
        var categories = new[]
        {
            NewCategory(events[0], "Sub-Junior", "Runners 14-17 years", 14, 17),
            NewCategory(events[0], "Junior", "Runners 18-22 years", 18, 22),
            NewCategory(events[0], "Senior", "Runners 23-39 years", 23, 39),
            NewCategory(events[0], "Veteran", "Runners 40-49 years", 40, 49),
            NewCategory(events[0], "Masters", "Runners 50+ years", 50, null),
            NewCategory(events[1], "Kids", "Children 8-12 years", 8, 12),
            NewCategory(events[1], "Teens", "Youth 13-17 years", 13, 17),
            NewCategory(events[1], "Adults", "Adults 18+ years", 18, null),
            NewCategory(events[2], "Under 16", "Walkers under 16", 10, 15),
            NewCategory(events[2], "Open", "Walkers 16-45 years", 16, 45),
            NewCategory(events[2], "Golden", "Walkers 46+ years", 46, null),
            NewCategory(events[3], "Junior", "Cyclists 16-19 years", 16, 19),
            NewCategory(events[3], "Elite", "Cyclists 20-34 years", 20, 34),
            NewCategory(events[3], "Masters", "Cyclists 35-49 years", 35, 49),
            NewCategory(events[3], "Grand Masters", "Cyclists 50+ years", 50, null),
        };

        // Enrolments as (participant no., event no., category no., status), using the Part 1 numbering.
        var enrolmentPlan = new (int Participant, int Event, int Category, string Status)[]
        {
            (1, 1, 3, EnrolmentStatuses.Confirmed),
            (1, 2, 8, EnrolmentStatuses.Confirmed),
            (2, 4, 13, EnrolmentStatuses.Pending),
            (2, 1, 4, EnrolmentStatuses.Confirmed),
            (3, 3, 10, EnrolmentStatuses.Confirmed),
            (3, 4, 14, EnrolmentStatuses.Confirmed),
            (4, 1, 3, EnrolmentStatuses.Pending),
            (4, 3, 11, EnrolmentStatuses.Confirmed),
            (5, 2, 7, EnrolmentStatuses.Confirmed),
            (5, 4, 13, EnrolmentStatuses.Confirmed),
        };

        var enrolments = enrolmentPlan.Select(plan => new Enrolment
        {
            Participant = participants[plan.Participant - 1],
            Event = events[plan.Event - 1],
            Category = categories[plan.Category - 1],
            Status = plan.Status,
            RegistrationDate = now,
        }).ToArray();

        // Results as (enrolment no., finish time, finish position), using the Part 1 numbering.
        var resultPlan = new (int Enrolment, string FinishTime, int Position)[]
        {
            (1, "03:15:30", 89),
            (2, "00:28:45", 34),
            (4, "04:45:20", 312),
            (5, "01:15:50", 56),
            (6, "02:55:10", 78),
            (8, "01:30:15", 45),
            (9, "00:32:20", 67),
            (10, "03:25:40", 92),
        };

        var results = resultPlan.Select(plan => new Result
        {
            Enrolment = enrolments[plan.Enrolment - 1],
            FinishTime = TimeSpan.Parse(plan.FinishTime),
            FinishPosition = plan.Position,
            Status = ResultStatuses.Published,
        }).ToArray();

        db.Organisers.AddRange(pretoria, durban);
        db.Participants.AddRange(participants);
        db.Events.AddRange(events);
        db.Categories.AddRange(categories);
        db.Enrolments.AddRange(enrolments);
        db.Results.AddRange(results);

        await db.SaveChangesAsync();
    }

    private static Category NewCategory(Event evt, string name, string description, int minAge, int? maxAge) =>
        new() { Event = evt, Name = name, Description = description, MinAge = minAge, MaxAge = maxAge };
}
