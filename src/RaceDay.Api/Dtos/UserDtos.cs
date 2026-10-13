using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>The profile returned by register, login and the profile endpoints. Never contains the password hash.</summary>
public record UserProfileResponse(
    int UserID,
    string Email,
    string FullName,
    string Role,
    string? OrganisationName,
    string? ContactNumber,
    DateOnly? DateOfBirth)
{
    /// <summary>Builds the profile from a user whose Organiser or Participant navigation is loaded.</summary>
    public static UserProfileResponse FromUser(User user) => new(
        user.UserID,
        user.Email,
        user.FullName,
        user.Role,
        user.Organiser?.OrganisationName,
        user.Organiser?.ContactNumber,
        user.Participant?.DateOfBirth);
}

/// <summary>
/// Body for <c>PUT /api/users/profile</c>. OrganisationName and ContactNumber apply only to Organisers;
/// DateOfBirth applies only to Participants. Fields that do not apply to the caller's role are ignored.
/// </summary>
public class UpdateProfileRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    public string? OrganisationName { get; set; }

    [StringLength(20)]
    public string? ContactNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }
}
