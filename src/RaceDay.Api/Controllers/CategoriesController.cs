using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>Event categories. Anyone can read them. Only the Organiser who owns the event can change them.</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class CategoriesController(RaceDayDbContext db) : RaceDayControllerBase
{
    /// <summary>Lists the categories for an event. Anyone can call this.</summary>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType<IReadOnlyList<CategoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetCategories(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.EventID == eventId))
        {
            return Problem(detail: $"Event {eventId} was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var categories = await db.Categories
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.MinAge)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return Ok(categories.Select(CategoryResponse.FromCategory).ToList());
    }

    /// <summary>Adds a category to an event the signed-in Organiser owns.</summary>
    [HttpPost("events/{eventId:int}/categories")]
    [SessionAuthorize(UserRoles.Organiser)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(int eventId, [FromBody] CategoryRequest request)
    {
        if (ValidateAgeRange(request) is { } ageError)
        {
            return Problem(detail: ageError, statusCode: StatusCodes.Status400BadRequest);
        }

        var (evt, failure) = await LoadOwnedEventAsync(db, eventId);
        if (failure is not null)
        {
            return failure;
        }

        var category = new Category
        {
            EventID = evt!.EventID,
            Event = evt,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategories), new { eventId }, CategoryResponse.FromCategory(category));
    }

    /// <summary>Updates a category. Only the Organiser who owns its event can do this.</summary>
    [HttpPut("categories/{id:int}")]
    [SessionAuthorize(UserRoles.Organiser)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(int id, [FromBody] CategoryRequest request)
    {
        if (ValidateAgeRange(request) is { } ageError)
        {
            return Problem(detail: ageError, statusCode: StatusCodes.Status400BadRequest);
        }

        var (category, failure) = await LoadOwnedCategoryAsync(id);
        if (failure is not null)
        {
            return failure;
        }

        category!.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;

        await db.SaveChangesAsync();

        return Ok(CategoryResponse.FromCategory(category));
    }

    /// <summary>Deletes a category. Returns 409 if participants are already enrolled in it.</summary>
    [HttpDelete("categories/{id:int}")]
    [SessionAuthorize(UserRoles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var (category, failure) = await LoadOwnedCategoryAsync(id);
        if (failure is not null)
        {
            return failure;
        }

        if (await db.Enrolments.AnyAsync(en => en.CategoryID == id))
        {
            return Problem(
                detail: "Participants are enrolled in this category, so it cannot be deleted.",
                statusCode: StatusCodes.Status409Conflict);
        }

        db.Categories.Remove(category!);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private static string? ValidateAgeRange(CategoryRequest request) =>
        request.MinAge is not null && request.MaxAge is not null && request.MinAge > request.MaxAge
            ? "minAge cannot be greater than maxAge."
            : null;

    private async Task<(Category? Category, ActionResult? Failure)> LoadOwnedCategoryAsync(int id)
    {
        var category = await db.Categories
            .Include(c => c.Event)
            .ThenInclude(e => e.Organiser)
            .FirstOrDefaultAsync(c => c.CategoryID == id);

        if (category is null)
        {
            return (null, Problem(detail: $"Category {id} was not found.", statusCode: StatusCodes.Status404NotFound));
        }

        if (category.Event.Organiser.UserID != CurrentUserId)
        {
            return (null, Problem(detail: "You can only manage categories for events that you created.", statusCode: StatusCodes.Status403Forbidden));
        }

        return (category, null);
    }
}
