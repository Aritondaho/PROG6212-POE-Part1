namespace RaceDay.Web.Models.Api;

// Shapes of the JSON returned and accepted by the RaceDay API (Part 2).
// The MVC project only exchanges these with the API over HTTP. It has no database access and no copy of the API's rules.

public record UserProfile(
    int UserID,
    string Email,
    string FullName,
    string Role,
    string? OrganisationName,
    string? ContactNumber,
    DateOnly? DateOfBirth,
    string? ProfilePictureUrl);

public record EventItem(
    int EventID,
    int OrganiserID,
    string OrganisationName,
    string Name,
    string Description,
    DateTime Date,
    string Location,
    decimal Distance,
    string EventType,
    string? BannerImageUrl);

public record OrganiserEventSummary(
    EventItem Event,
    int EnrolmentCount,
    int ConfirmedCount,
    int CategoryCount,
    int ResultCount);

public record CategoryItem(int CategoryID, int EventID, string Name, string? Description, int? MinAge, int? MaxAge);

public record EnrolmentItem(
    int EnrolmentID,
    int EventID,
    string EventName,
    DateTime EventDate,
    int CategoryID,
    string CategoryName,
    int ParticipantID,
    string ParticipantName,
    string Status,
    DateTime RegistrationDate);

public record ResultItem(
    int ResultID,
    int EnrolmentID,
    int EventID,
    string EventName,
    DateTime EventDate,
    string CategoryName,
    string ParticipantName,
    TimeSpan FinishTime,
    int FinishPosition,
    int TotalFinishers,
    string Status);

public record BannerUpload(string ImageUrl);

public record RegisterBody(
    string Email,
    string Password,
    string FullName,
    string Role,
    string? OrganisationName,
    string? ContactNumber,
    DateOnly? DateOfBirth);

public record LoginBody(string Email, string Password);

public record UpdateProfileBody(
    string FullName,
    string Email,
    string? OrganisationName,
    string? ContactNumber,
    DateOnly? DateOfBirth);

public record EventBody(
    string Name,
    string Description,
    DateTime Date,
    string Location,
    decimal Distance,
    string EventType);

public record CategoryBody(string Name, string? Description, int? MinAge, int? MaxAge);

public record EnrolBody(int CategoryId);

public record EnrolmentStatusBody(string Status);

public record CreateResultBody(int EnrolmentId, TimeSpan FinishTime, int FinishPosition);

public record UpdateResultBody(TimeSpan FinishTime, int FinishPosition);

/// <summary>Values the API accepts. Used to fill drop-down lists, not to validate.</summary>
public static class RaceDayValues
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static readonly string[] EventTypes = ["Run", "Walk", "Cycle"];
    public static readonly string[] EnrolmentStatuses = ["Pending", "Confirmed", "Cancelled"];
}
