namespace RaceDay.Api.Models;

/// <summary>
/// A login account. Every user is either an Organiser or a Participant, and
/// the matching profile row lives in <see cref="Organiser"/> or <see cref="Participant"/>.
/// </summary>
public class User
{
    public int UserID { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash produced by PasswordHasher. The plain-text password is never stored.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    /// <summary>Either <see cref="UserRoles.Organiser"/> or <see cref="UserRoles.Participant"/>.</summary>
    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Organiser? Organiser { get; set; }

    public Participant? Participant { get; set; }
}
