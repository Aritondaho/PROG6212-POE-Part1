using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>Body for <c>POST /api/events/{eventId}/enrol</c>. The category must belong to that event.</summary>
public class EnrolRequest
{
    [Required]
    public int CategoryId { get; set; }
}

/// <summary>Body for <c>PUT /api/enrolments/{id}/status</c>. Allowed values: Pending, Confirmed, Cancelled.</summary>
public class UpdateEnrolmentStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

/// <summary>An enrolment linking one participant, one event and one category.</summary>
public record EnrolmentResponse(
    int EnrolmentID,
    int EventID,
    string EventName,
    DateTime EventDate,
    int CategoryID,
    string CategoryName,
    int ParticipantID,
    string ParticipantName,
    string Status,
    DateTime RegistrationDate)
{
    /// <summary>Builds the response from an enrolment with Participant.User, Event and Category loaded.</summary>
    public static EnrolmentResponse FromEnrolment(Enrolment enrolment) => new(
        enrolment.EnrolmentID,
        enrolment.EventID,
        enrolment.Event.Name,
        enrolment.Event.EventDate,
        enrolment.CategoryID,
        enrolment.Category.Name,
        enrolment.ParticipantID,
        enrolment.Participant.User.FullName,
        enrolment.Status,
        enrolment.RegistrationDate);
}
