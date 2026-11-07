using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models;

/// <summary>Create and edit form for an event, including the optional banner image upload.</summary>
public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Give the event a name.")]
    [StringLength(100)]
    [Display(Name = "Event name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Describe the event.")]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose the date and start time.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Date and start time")]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Enter where the event takes place.")]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the distance.")]
    [Range(0.01, 999.99, ErrorMessage = "Distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? Distance { get; set; }

    [Required(ErrorMessage = "Choose Run, Walk or Cycle.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? CurrentBannerUrl { get; set; }

    public bool IsEdit => EventId is not null;

    public EventBody ToBody() => new(Name.Trim(), Description.Trim(), Date!.Value, Location.Trim(), Distance!.Value, EventType);

    public static EventFormViewModel From(EventItem evt) => new()
    {
        EventId = evt.EventID,
        Name = evt.Name,
        Description = evt.Description,
        Date = evt.Date,
        Location = evt.Location,
        Distance = evt.Distance,
        EventType = evt.EventType,
        CurrentBannerUrl = evt.BannerImageUrl,
    };
}
