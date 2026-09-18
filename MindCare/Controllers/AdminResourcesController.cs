using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;
using MindCare.ViewModels;

namespace MindCare.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class AdminResourcesController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index() => View(await context.Resources.Include(r => r.ResourceCategory).Include(r => r.CreatedByUser).OrderByDescending(r => r.UpdatedAt).ToListAsync());
    public async Task<IActionResult> Pending() => View(await context.Resources.Include(r => r.ResourceCategory).Include(r => r.CreatedByUser).Where(r => r.Status == ResourceStatus.PendingReview).OrderBy(r => r.SubmittedAt).ToListAsync());
    public async Task<IActionResult> Create() => View(await Form());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ResourceEditViewModel model, string command)
    {
        if (!ModelState.IsValid) { model.Categories = await Categories(); return View(model); }
        var now = DateTime.UtcNow;
        var resource = new Resource { CreatedAt = now, UpdatedAt = now, CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier), CreatedByRole = RoleNames.Admin };
        Map(resource, model);
        SetForCreate(resource, command);
        context.Resources.Add(resource);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var resource = await context.Resources.FindAsync(id);
        if (resource is null) return NotFound();
        if (resource.Status is ResourceStatus.PendingReview or ResourceStatus.Archived) return BadRequest();
        ViewData["ResourceStatus"] = resource.Status;
        return View(await Form(resource));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ResourceEditViewModel model, string command)
    {
        var resource = await context.Resources.FindAsync(id);
        if (resource is null) return NotFound();
        if (resource.Status is ResourceStatus.PendingReview or ResourceStatus.Archived) return BadRequest();
        if (!ModelState.IsValid) { model.Categories = await Categories(model.CategoryId); ViewData["ResourceStatus"] = resource.Status; return View(model); }

        Map(resource, model);
        resource.UpdatedAt = DateTime.UtcNow;
        if (resource.Status != ResourceStatus.Published) SetForCreate(resource, command);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Review(int id)
    {
        var resource = await context.Resources.Include(r => r.ResourceCategory).Include(r => r.CreatedByUser).SingleOrDefaultAsync(r => r.Id == id && r.Status == ResourceStatus.PendingReview);
        return resource is null ? NotFound() : View(resource);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var resource = await context.Resources.FindAsync(id);
        if (resource is null) return NotFound();
        if (resource.Status != ResourceStatus.PendingReview) return BadRequest();

        var normalizedTitle = resource.Title.Trim().ToUpper();
        var matchingPublishedResource = await context.Resources.AnyAsync(candidate =>
            candidate.Id != resource.Id &&
            candidate.Status == ResourceStatus.Published &&
            candidate.CategoryId == resource.CategoryId &&
            candidate.Title.ToUpper() == normalizedTitle &&
            candidate.Content == resource.Content);
        if (matchingPublishedResource)
        {
            TempData["ErrorMessage"] = "An identical resource is already published. This submission was not approved.";
            return RedirectToAction(nameof(Review), new { id });
        }

        Publish(resource);
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Resource approved and published.";
        return RedirectToAction(nameof(Pending));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(ResourceReviewViewModel model)
    {
        var resource = await context.Resources.FindAsync(model.Id);
        if (resource is null) return NotFound();
        if (resource.Status != ResourceStatus.PendingReview || !ModelState.IsValid) return BadRequest();
        resource.Status = ResourceStatus.Rejected;
        resource.ReviewedAt = resource.UpdatedAt = DateTime.UtcNow;
        resource.ReviewedByAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        resource.ReviewNote = model.ReviewNote.Trim();
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Resource returned to the counsellor for revision.";
        return RedirectToAction(nameof(Pending));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var resource = await context.Resources.FindAsync(id);
        if (resource is null) return NotFound();
        if (resource.Status is not (ResourceStatus.Published or ResourceStatus.Draft or ResourceStatus.Rejected)) return BadRequest();
        resource.Status = ResourceStatus.Archived;
        resource.ArchivedAt = resource.UpdatedAt = DateTime.UtcNow;
        resource.IsFeatured = false;
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var resource = await context.Resources.FindAsync(id);
        if (resource is null) return NotFound();
        if (resource.Status != ResourceStatus.Archived) return BadRequest();
        resource.Status = ResourceStatus.Draft;
        resource.UpdatedAt = DateTime.UtcNow;
        resource.ArchivedAt = null;
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private Task<List<ResourceCategory>> Categories(int? includeCategoryId = null) => context.ResourceCategories
        .Where(category => category.IsActive || category.Id == includeCategoryId)
        .OrderBy(category => category.Name)
        .ToListAsync();

    private async Task<ResourceEditViewModel> Form(Resource? resource = null)
    {
        var categoryId = resource?.CategoryId;

        // Older seeded resources can still have only the legacy Category name.
        // Resolve it for the edit form without modifying the stored resource.
        if (!categoryId.HasValue && !string.IsNullOrWhiteSpace(resource?.Category))
        {
            categoryId = await context.ResourceCategories
                .Where(category => category.Name == resource.Category)
                .Select(category => (int?)category.Id)
                .FirstOrDefaultAsync();
        }

        return new ResourceEditViewModel
        {
            Id = resource?.Id ?? 0,
            Title = resource?.Title ?? "",
            Description = resource?.Description ?? "",
            CategoryId = categoryId,
            Content = resource?.Content ?? "",
            Url = resource?.Url,
            IsFeatured = resource?.IsFeatured ?? false,
            Categories = await Categories(categoryId)
        };
    }

    private static void Map(Resource resource, ResourceEditViewModel model)
    {
        resource.Title = model.Title.Trim();
        resource.Description = model.Description.Trim();
        resource.Content = model.Content.Trim();
        resource.CategoryId = model.CategoryId;
        resource.Url = model.Url?.Trim();
        resource.IsFeatured = model.IsFeatured;
    }

    private void SetForCreate(Resource resource, string command)
    {
        if (command == "publish") Publish(resource);
        else resource.Status = ResourceStatus.Draft;
    }

    private void Publish(Resource resource)
    {
        var now = DateTime.UtcNow;
        resource.Status = ResourceStatus.Published;
        resource.PublishedAt ??= now;
        resource.ReviewedAt = now;
        resource.ReviewedByAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        resource.ReviewNote = null;
        resource.UpdatedAt = now;
    }
}
