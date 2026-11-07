namespace RaceDay.Web.Services;

/// <summary>Settings for the "RaceDayApi" configuration section.</summary>
public class RaceDayApiOptions
{
    public const string SectionName = "RaceDayApi";

    /// <summary>
    /// Address the MVC server uses to call the API, for example http://localhost:5285/.
    /// Inside Docker this is the API container or http://host.docker.internal:5285/.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5285/";

    /// <summary>
    /// Address the visitor's browser uses to load images that the API stores locally (relative "/uploads/..." URLs).
    /// Images in Azure Blob Storage already have absolute URLs and ignore this. Defaults to <see cref="BaseUrl"/>.
    /// </summary>
    public string? PublicUrl { get; set; }
}
