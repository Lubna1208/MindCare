using System.Security.Claims;
using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
using MindCare.Data; using MindCare.Models; using MindCare.ViewModels;
namespace MindCare.Controllers;
[Authorize(Roles = RoleNames.Counsellor)]
public class CounsellorResourcesController(ApplicationDbContext context) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public async Task<IActionResult> Index() => View(await context.Resources.Include(r => r.ResourceCategory).Where(r => r.CreatedByUserId == UserId).OrderByDescending(r => r.UpdatedAt).ToListAsync());
    public async Task<IActionResult> Create() => View(await Form());
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Create(ResourceEditViewModel model, string command)
    {
        if (!ValidForSubmit(model, command)) { model.Categories = await Categories(); return View(model); }
        var now = DateTime.UtcNow; var resource = new Resource { CreatedByUserId = UserId, CreatedByRole = RoleNames.Counsellor, CreatedAt = now, UpdatedAt = now };
        Map(resource, model); SetSubmission(resource, command, now); context.Resources.Add(resource); await context.SaveChangesAsync();
        TempData["SuccessMessage"] = resource.Status == ResourceStatus.PendingReview ? "Resource submitted for review." : "Resource saved as draft."; return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id)
    {
        var r = await Owned(id); if (r is null) return NotFound(); if (r.Status is not (ResourceStatus.Draft or ResourceStatus.Rejected)) return Forbid();
        return View(await Form(r));
    }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Edit(int id, ResourceEditViewModel model, string command)
    {
        var r = await Owned(id); if (r is null) return NotFound(); if (r.Status is not (ResourceStatus.Draft or ResourceStatus.Rejected)) return Forbid();
        if (!ValidForSubmit(model, command)) { model.Categories = await Categories(); return View(model); }
        Map(r, model); r.UpdatedAt = DateTime.UtcNow; SetSubmission(r, command, r.UpdatedAt); await context.SaveChangesAsync();
        TempData["SuccessMessage"] = r.Status == ResourceStatus.PendingReview ? "Resource submitted for review." : "Resource saved as draft."; return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Submit(int id)
    { var r = await Owned(id); if (r is null) return NotFound(); if (r.Status is not (ResourceStatus.Draft or ResourceStatus.Rejected) || !Complete(r)) return BadRequest(); r.Status=ResourceStatus.PendingReview; r.SubmittedAt=r.UpdatedAt=DateTime.UtcNow; await context.SaveChangesAsync(); TempData["SuccessMessage"]="Resource submitted for review."; return RedirectToAction(nameof(Index)); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Archive(int id)
    { var r=await Owned(id); if(r is null)return NotFound(); if(r.Status is not (ResourceStatus.Draft or ResourceStatus.Rejected))return Forbid(); r.Status=ResourceStatus.Archived;r.ArchivedAt=r.UpdatedAt=DateTime.UtcNow;await context.SaveChangesAsync();return RedirectToAction(nameof(Index)); }
    private async Task<Resource?> Owned(int id) => await context.Resources.Include(r=>r.ResourceCategory).SingleOrDefaultAsync(r=>r.Id==id && r.CreatedByUserId==UserId);
    private Task<List<ResourceCategory>> Categories()=>context.ResourceCategories.Where(c=>c.IsActive).OrderBy(c=>c.Name).ToListAsync();
    private async Task<ResourceEditViewModel> Form(Resource? r=null)=>new(){Id=r?.Id??0,Title=r?.Title??"",Description=r?.Description??"",CategoryId=r?.CategoryId,Content=r?.Content??"",Url=r?.Url,Categories=await Categories()};
    private bool ValidForSubmit(ResourceEditViewModel m,string c){ if(c=="submit" && !ModelState.IsValid)return false; return true; }
    private static bool Complete(Resource r)=>!string.IsNullOrWhiteSpace(r.Title)&&!string.IsNullOrWhiteSpace(r.Description)&&!string.IsNullOrWhiteSpace(r.Content)&&r.CategoryId.HasValue;
    private static void Map(Resource r,ResourceEditViewModel m){r.Title=m.Title.Trim();r.Description=m.Description.Trim();r.Content=m.Content.Trim();r.CategoryId=m.CategoryId;r.Url=m.Url?.Trim();}
    private static void SetSubmission(Resource r,string c,DateTime now){if(c=="submit"){r.Status=ResourceStatus.PendingReview;r.SubmittedAt=now;}else r.Status=ResourceStatus.Draft;}
}
