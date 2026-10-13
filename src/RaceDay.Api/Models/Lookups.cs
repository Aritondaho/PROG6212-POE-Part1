namespace RaceDay.Api.Models;

/// <summary>Allowed values for <see cref="User.Role"/>. Matches the CHECK constraint in the Part 1 SQL script.</summary>
public static class UserRoles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

/// <summary>Allowed values for <see cref="Event.EventType"/>.</summary>
public static class EventTypes
{
    public const string Run = "Run";
    public const string Walk = "Walk";
    public const string Cycle = "Cycle";

    public static readonly string[] All = [Run, Walk, Cycle];
}

/// <summary>Allowed values for <see cref="Enrolment.Status"/>.</summary>
public static class EnrolmentStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Pending, Confirmed, Cancelled];
}

/// <summary>Allowed values for <see cref="Result.Status"/>.</summary>
public static class ResultStatuses
{
    public const string Published = "Published";
    public const string Pending = "Pending";
    public const string Disqualified = "Disqualified";
}
