namespace RaceDay.Api.Services;

/// <summary>
/// Stores uploaded images and returns the URL that is saved in the database.
/// <see cref="LocalImageStorage"/> writes to wwwroot (Part 2 behaviour, used by the tests and when Azure is not configured).
/// Part 3 adds an Azure Blob Storage implementation. Controllers only see this interface.
/// </summary>
public interface IImageStorage
{
    /// <summary>Saves the file in the given folder (for example "banners") and returns its URL.</summary>
    Task<string> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
}

/// <summary>Folder names used for each kind of image.</summary>
public static class ImageFolders
{
    public const string Banners = "banners";
    public const string ProfilePictures = "profile-pictures";
}

/// <summary>Limits applied to every uploaded image (event banners and profile pictures).</summary>
public static class ImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024;

    /// <summary>Accepted content types, mapped to the file extension used when saving.</summary>
    public static readonly IReadOnlyDictionary<string, string> ExtensionsByContentType = new Dictionary<string, string>
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    /// <summary>Returns an error message when the file is not an accepted image, otherwise null.</summary>
    public static string? Validate(IFormFile file, string label)
    {
        if (!ExtensionsByContentType.ContainsKey(file.ContentType.ToLowerInvariant()))
        {
            return $"{label} must be a JPEG, PNG or WebP image.";
        }

        if (file.Length == 0 || file.Length > MaxBytes)
        {
            return $"{label} must be between 1 byte and 5 MB.";
        }

        return null;
    }

    /// <summary>A random file name with the right extension, so user-supplied names are never used.</summary>
    public static string NewFileName(IFormFile file) =>
        $"{Guid.NewGuid():N}{ExtensionsByContentType[file.ContentType.ToLowerInvariant()]}";
}
