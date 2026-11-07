using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Services;

/// <summary>Settings for the "AzureBlobStorage" configuration section.</summary>
public class AzureBlobStorageOptions
{
    public const string SectionName = "AzureBlobStorage";

    /// <summary>
    /// Storage account connection string from the Azure portal (Storage account, Access keys).
    /// Keep it out of source control: use user-secrets locally and an environment variable in Docker or CI.
    /// When it is empty the API falls back to <see cref="LocalImageStorage"/>.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Container that holds the images. It is created on first upload if it does not exist.</summary>
    public string ContainerName { get; set; } = "raceday-images";
}

/// <summary>
/// Stores images in Azure Blob Storage as {folder}/{random-name}.{ext} and returns the blob's URL,
/// which is saved in the database and shown by the MVC application.
/// <para>
/// The container is created with anonymous read access for blobs so the browser can load the images directly.
/// Many storage accounts block anonymous access. In that case the container is created as private and the
/// returned URL carries a long-lived, read-only SAS token instead, so images still display without exposing the key.
/// </para>
/// </summary>
public sealed class AzureBlobImageStorage : IImageStorage
{
    private static readonly TimeSpan SasLifetime = TimeSpan.FromDays(365 * 5);

    private readonly BlobContainerClient _container;
    private readonly ILogger<AzureBlobImageStorage> _logger;
    private readonly SemaphoreSlim _initialiseLock = new(1, 1);
    private bool _initialised;
    private bool _anonymousRead;

    public AzureBlobImageStorage(IOptions<AzureBlobStorageOptions> options, ILogger<AzureBlobImageStorage> logger)
    {
        var settings = options.Value;
        _container = new BlobContainerClient(settings.ConnectionString, settings.ContainerName);
        _logger = logger;
    }

    public async Task<string> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        await EnsureContainerAsync(cancellationToken);

        var blob = _container.GetBlobClient($"{folder}/{ImageRules.NewFileName(file)}");

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = file.ContentType.ToLowerInvariant(),
                // File names are random and never reused, so browsers may cache them for a long time.
                CacheControl = "public, max-age=31536000",
            },
        }, cancellationToken);

        if (_anonymousRead)
        {
            return blob.Uri.ToString();
        }

        if (!blob.CanGenerateSasUri)
        {
            throw new InvalidOperationException(
                "The blob container is private and the connection string has no account key, so a read URL cannot be created. " +
                "Use the storage account's connection string with AccountKey, or allow anonymous blob access on the account.");
        }

        return blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(SasLifetime)).ToString();
    }

    /// <summary>Creates the container once per process and records whether blobs can be read anonymously.</summary>
    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (_initialised)
        {
            return;
        }

        await _initialiseLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialised)
            {
                return;
            }

            try
            {
                await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == "PublicAccessNotPermitted")
            {
                _logger.LogInformation(
                    "Anonymous blob access is disabled on this storage account. Creating a private container and using SAS URLs.");
                await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
            }

            var properties = await _container.GetPropertiesAsync(cancellationToken: cancellationToken);
            _anonymousRead = properties.Value.PublicAccess is PublicAccessType.Blob or PublicAccessType.BlobContainer;
            _initialised = true;
        }
        finally
        {
            _initialiseLock.Release();
        }
    }
}
