namespace RaceDay.Api.Services;

/// <summary>
/// Stores event banner images and returns the URL that is saved on the event.
/// Part 2 uses <see cref="LocalBannerStorage"/>. Part 3 replaces it with Azure Blob Storage
/// behind the same interface, so the controller does not change.
/// </summary>
public interface IBannerStorage
{
    /// <summary>Saves the file and returns its URL.</summary>
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);
}

/// <summary>Limits applied to uploaded banner images.</summary>
public static class BannerImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024;

    /// <summary>Accepted content types, mapped to the file extension used when saving.</summary>
    public static readonly IReadOnlyDictionary<string, string> ExtensionsByContentType = new Dictionary<string, string>
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };
}
