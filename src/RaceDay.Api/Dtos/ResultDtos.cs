using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>Body for <c>POST /api/events/{eventId}/results</c>.</summary>
public class CreateResultRequest
{
    /// <summary>The enrolment to record a result for. It must belong to the event in the URL.</summary>
    [Range(1, int.MaxValue)]
    public int EnrolmentId { get; set; }

    /// <summary>Finish time in hh:mm:ss, greater than zero and less than 24 hours.</summary>
    [Required]
    public TimeSpan FinishTime { get; set; }

    /// <summary>Finishing position, starting at 1.</summary>
    [Range(1, int.MaxValue)]
    public int FinishPosition { get; set; }
}

/// <summary>Body for <c>PUT /api/results/{id}</c>.</summary>
public class UpdateResultRequest
{
    [Required]
    public TimeSpan FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int FinishPosition { get; set; }
}

/// <summary>
/// A recorded result. TotalFinishers is the number of published results for the event,
/// so a participant's position can be shown as "47th out of 312 finishers".
/// </summary>
public record ResultResponse(
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
    string Status)
{
    /// <summary>Builds the response from a result with Enrolment, Event, Category and Participant.User loaded.</summary>
    public static ResultResponse FromResult(Result result, int totalFinishers) => new(
        result.ResultID,
        result.EnrolmentID,
        result.Enrolment.EventID,
        result.Enrolment.Event.Name,
        result.Enrolment.Event.EventDate,
        result.Enrolment.Category.Name,
        result.Enrolment.Participant.User.FullName,
        result.FinishTime,
        result.FinishPosition,
        totalFinishers,
        result.Status);
}
