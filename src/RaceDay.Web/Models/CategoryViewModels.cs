using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models;

/// <summary>Add or edit an age or distance category, for example "Under 20", "Senior" or "21km".</summary>
public class CategoryFormViewModel : IValidatableObject
{
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Give the category a name.")]
    [StringLength(50)]
    [Display(Name = "Category name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    [Range(0, 150, ErrorMessage = "Enter an age between 0 and 150.")]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 150, ErrorMessage = "Enter an age between 0 and 150.")]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    public CategoryBody ToBody() => new(Name.Trim(), string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(), MinAge, MaxAge);

    public static CategoryFormViewModel From(CategoryItem c) => new()
    {
        CategoryId = c.CategoryID,
        Name = c.Name,
        Description = c.Description,
        MinAge = c.MinAge,
        MaxAge = c.MaxAge,
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinAge is not null && MaxAge is not null && MinAge > MaxAge)
        {
            yield return new ValidationResult("The minimum age can't be more than the maximum age.", [nameof(MaxAge)]);
        }
    }
}

public class CategoriesPageViewModel
{
    public required OrganiserEventSummary Summary { get; init; }

    public IReadOnlyList<CategoryItem> Categories { get; init; } = [];

    /// <summary>Number of active enrolments per category, used to show which categories can be deleted.</summary>
    public IReadOnlyDictionary<int, int> EnrolmentsByCategory { get; init; } = new Dictionary<int, int>();

    public CategoryFormViewModel NewCategory { get; init; } = new();
}
