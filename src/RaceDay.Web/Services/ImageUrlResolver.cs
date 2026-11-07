using Microsoft.Extensions.Options;

namespace RaceDay.Web.Services;

/// <summary>
/// Makes image URLs from the API usable in the browser. Azure Blob Storage URLs are already absolute.
/// Images the API stored on its own disk come back as "/uploads/...", so they are prefixed with the API's public address.
/// </summary>
public class ImageUrlResolver(IOptions<RaceDayApiOptions> options)
{
    private readonly string _publicBase = (string.IsNullOrWhiteSpace(options.Value.PublicUrl)
        ? options.Value.BaseUrl
        : options.Value.PublicUrl!).TrimEnd('/');

    public string? Resolve(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out _) ? url : $"{_publicBase}/{url.TrimStart('/')}";
    }
}
