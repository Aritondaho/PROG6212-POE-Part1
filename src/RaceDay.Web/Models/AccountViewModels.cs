using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

/// <summary>
/// Registration form. These checks only give quick feedback on the page; the API validates everything again
/// and its messages are shown if it rejects the request (for example, an email that is already registered).
/// </summary>
public class RegisterViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Choose how you will use RaceDay.")]
    public string Role { get; set; } = RaceDayValues.Participant;

    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(255)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role == RaceDayValues.Organiser)
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
        else if (Role == RaceDayValues.Participant)
        {
            if (DateOfBirth is null)
            {
                yield return new ValidationResult("Enter your date of birth.", [nameof(DateOfBirth)]);
            }
            else if (DateOfBirth.Value >= DateOnly.FromDateTime(DateTime.Today))
            {
                yield return new ValidationResult("Your date of birth must be in the past.", [nameof(DateOfBirth)]);
            }
        }
        else
        {
            yield return new ValidationResult("Choose Organiser or Participant.", [nameof(Role)]);
        }
    }
}
