using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>Body for <c>POST /api/auth/register</c>.</summary>
public class RegisterRequest
{
    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Plain-text password. It is hashed before it is stored and never returned.</summary>
    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Either "Organiser" or "Participant".</summary>
    [Required]
    public string Role { get; set; } = string.Empty;

    /// <summary>Required when Role is Organiser.</summary>
    [StringLength(100)]
    public string? OrganisationName { get; set; }

    /// <summary>Required when Role is Organiser.</summary>
    [StringLength(20)]
    public string? ContactNumber { get; set; }

    /// <summary>Required when Role is Participant. Must be in the past.</summary>
    public DateOnly? DateOfBirth { get; set; }
}

/// <summary>Body for <c>POST /api/auth/login</c>.</summary>
public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
