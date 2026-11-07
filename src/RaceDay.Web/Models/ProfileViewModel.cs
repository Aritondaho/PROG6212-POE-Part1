using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models;

/// <summary>The profile form. Which fields appear depends on the role the API reports for the user.</summary>
public class ProfileViewModel : IValidatableObject
{
    public string Role { get; set; } = string.Empty;

    public string? ProfilePictureUrl { get; set; }

    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(255)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Organisation name")]
    public string? OrganisationName { get; set; }

    [StringLength(20)]
    [Phone(ErrorMessage = "Enter a valid contact number.")]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    public bool IsOrganiser => Role == RaceDayValues.Organiser;

    public static ProfileViewModel From(UserProfile profile) => new()
    {
        Role = profile.Role,
        ProfilePictureUrl = profile.ProfilePictureUrl,
        FullName = profile.FullName,
        Email = profile.Email,
        OrganisationName = profile.OrganisationName,
        ContactNumber = profile.ContactNumber,
        DateOfBirth = profile.DateOfBirth,
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsOrganiser)
        {
            if (string.IsNullOrWhiteSpace(OrganisationName))
            {
                yield return new ValidationResult("Enter your organisation's name.", [nameof(OrganisationName)]);
            }

            if (string.IsNullOrWhiteSpace(ContactNumber))
            {
                yield return new ValidationResult("Enter a contact number.", [nameof(ContactNumber)]);
            }
        }
        else if (DateOfBirth is null)
        {
            yield return new ValidationResult("Enter your date of birth.", [nameof(DateOfBirth)]);
        }
        else if (DateOfBirth.Value >= DateOnly.FromDateTime(DateTime.Today))
        {
            yield return new ValidationResult("Your date of birth must be in the past.", [nameof(DateOfBirth)]);
        }
    }
}
