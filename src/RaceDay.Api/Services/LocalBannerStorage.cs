namespace RaceDay.Api.Services;

/// <summary>
/// Saves banner images under wwwroot/uploads/banners with a random file name, so user-supplied names are never used.
/// The returned URL is relative to the API host and is served by UseStaticFiles.
/// </summary>
public class LocalBannerStorage(IWebHostEnvironment environment) : IBannerStorage
{
    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var extension = BannerImageRules.ExtensionsByContentType[file.ContentType.ToLowerInvariant()];
        var fileName = $"{Guid.NewGuid():N}{extension}";

        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "banners");
        Directory.CreateDirectory(folder);

        await using var stream = File.Create(Path.Combine(folder, fileName));
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/banners/{fileName}";
    }
}
