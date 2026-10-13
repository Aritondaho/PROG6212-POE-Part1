using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>The profile returned by register, login and the profile endpoints. Never contains the password hash.</summary>
public record UserProfileResponse(
    int UserID,
    string Email,
    string FullName,
    string Role,
    string? OrganisationName,
    string? ContactNumber,
    DateOnly? DateOfBirth);

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
