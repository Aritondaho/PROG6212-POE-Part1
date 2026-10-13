using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>Body for <c>POST /api/events/{eventId}/categories</c> and <c>PUT /api/categories/{id}</c>.</summary>
public class CategoryRequest
{
    /// <summary>For example "Senior", "Under 20" or "10km".</summary>
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    /// <summary>Lowest age in the category. Leave out for distance-based categories.</summary>
    [Range(0, 150)]
    public int? MinAge { get; set; }

    /// <summary>Highest age in the category. Leave out for an open-ended category such as "Masters 50+".</summary>
    [Range(0, 150)]
    public int? MaxAge { get; set; }
}

/// <summary>A category as returned by the API.</summary>
public record CategoryResponse(int CategoryID, int EventID, string Name, string? Description, int? MinAge, int? MaxAge)
{
    public static CategoryResponse FromCategory(Category category) => new(
        category.CategoryID,
        category.EventID,
        category.Name,
        category.Description,
        category.MinAge,
        category.MaxAge);
}
