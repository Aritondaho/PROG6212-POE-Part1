using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>Body for <c>POST /api/events</c> and <c>PUT /api/events/{id}</c>.</summary>
public class EventRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Date and time the event starts.</summary>
    [Required]
    public DateTime Date { get; set; }

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    /// <summary>Distance in kilometres, between 0.01 and 999.99.</summary>
    [Range(0.01, 999.99)]
    public decimal Distance { get; set; }

    /// <summary>Run, Walk or Cycle.</summary>
    [Required]
    public string EventType { get; set; } = string.Empty;

    /// <summary>Optional banner URL. Leave it out to keep the current banner. Use the upload endpoint to set one from a file.</summary>
    [StringLength(500)]
    public string? BannerImageUrl { get; set; }
}

/// <summary>An event as returned by the API.</summary>
public record EventResponse(
    int EventID,
    int OrganiserID,
    string OrganisationName,
    string Name,
    string Description,
    DateTime Date,
    string Location,
    decimal Distance,
    string EventType,
    string? BannerImageUrl)
{
    /// <summary>Builds the response from an event whose Organiser is loaded.</summary>
    public static EventResponse FromEvent(Event evt) => new(
        evt.EventID,
        evt.OrganiserID,
        evt.Organiser.OrganisationName,
        evt.Name,
        evt.Description,
        evt.EventDate,
        evt.Location,
        evt.Distance,
        evt.EventType,
        evt.BannerImageURL);
}
