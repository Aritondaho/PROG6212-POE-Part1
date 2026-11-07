namespace RaceDay.Api.Services;

/// <summary>
/// Saves images under wwwroot/uploads/{folder} with a random file name.
/// The returned URL is relative to the API host and is served by UseStaticFiles.
/// Used when no Azure Blob Storage connection string is configured, and by the automated tests.
/// </summary>
public class LocalImageStorage(IWebHostEnvironment environment) : IImageStorage
{
    public async Task<string> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        var fileName = ImageRules.NewFileName(file);

        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var directory = Path.Combine(webRoot, "uploads", folder);
        Directory.CreateDirectory(directory);

        await using var stream = File.Create(Path.Combine(directory, fileName));
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/{folder}/{fileName}";
    }
}
