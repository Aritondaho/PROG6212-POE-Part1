using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organisers add, edit and remove the age or distance categories for one of their events.</summary>
public class CategoriesController(RaceDayApiClient api) : OrganiserEventControllerBase(api)
{
    [HttpGet]
    public async Task<IActionResult> Index(int eventId, CancellationToken ct)
    {
        var page = await BuildPageAsync(eventId, new CategoryFormViewModel(), ct);
        return page is null ? NotFoundPage() : View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int eventId, [Bind(Prefix = "NewCategory")] CategoryFormViewModel form, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var created = await Api.CreateCategoryAsync(eventId, form.ToBody(), ct);
                FlashSuccess($"Category '{created.Name}' added.");
                return RedirectToAction(nameof(Index), new { eventId });
            }
            catch (ApiException ex)
            {
                AddApiError(ex);
            }
        }

        var page = await BuildPageAsync(eventId, form, ct);
        return page is null ? NotFoundPage() : View("Index", page);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int eventId, int id, CancellationToken ct)
    {
        var summary = await FindOwnEventAsync(eventId, ct);
        var category = summary is null ? null : (await Api.GetCategoriesAsync(eventId, ct)).FirstOrDefault(c => c.CategoryID == id);
        if (summary is null || category is null)
        {
            return NotFoundPage();
        }

        ViewData["Title"] = $"Edit {category.Name}";
        ViewData["Summary"] = summary;
        return View(CategoryFormViewModel.From(category));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int eventId, int id, CategoryFormViewModel form, CancellationToken ct)
    {
        form.CategoryId = id;
        if (ModelState.IsValid)
        {
            try
            {
                await Api.UpdateCategoryAsync(id, form.ToBody(), ct);
                FlashSuccess("Category updated.");
                return RedirectToAction(nameof(Index), new { eventId });
            }
            catch (ApiException ex)
            {
                AddApiError(ex);
            }
        }

        var summary = await FindOwnEventAsync(eventId, ct);
        if (summary is null)
        {
            return NotFoundPage();
        }

        ViewData["Title"] = $"Edit {form.Name}";
        ViewData["Summary"] = summary;
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int eventId, int id, CancellationToken ct)
    {
        try
        {
            await Api.DeleteCategoryAsync(id, ct);
            FlashSuccess("Category removed.");
        }
        catch (ApiException ex) when (ex is not ApiUnauthorizedException)
        {
            // 409 when participants are already entered in this category.
            FlashError(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<CategoriesPageViewModel?> BuildPageAsync(int eventId, CategoryFormViewModel form, CancellationToken ct)
    {
        var summary = await FindOwnEventAsync(eventId, ct);
        if (summary is null)
        {
            return null;
        }

        var categories = await Api.GetCategoriesAsync(eventId, ct);
        var enrolments = await Api.GetEventEnrolmentsAsync(eventId, ct);

        ViewData["Title"] = $"Categories · {summary.Event.Name}";
        return new CategoriesPageViewModel
        {
            Summary = summary,
            Categories = categories,
            EnrolmentsByCategory = enrolments.GroupBy(en => en.CategoryID).ToDictionary(g => g.Key, g => g.Count()),
            NewCategory = form,
        };
    }
}
