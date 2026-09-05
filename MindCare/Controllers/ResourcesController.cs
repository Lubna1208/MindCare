using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;

namespace MindCare.Controllers;

[AllowAnonymous]
public class ResourcesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ResourcesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? category, string? search)
    {
        var resources = _context.Resources.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category) && ResourceCategories.All.Contains(category))
        {
            resources = resources.Where(resource => resource.Category == category);
        }
        else
        {
            category = null;
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            resources = resources.Where(resource =>
                resource.Title.Contains(search) ||
                resource.Description.Contains(search) ||
                resource.Category.Contains(search));
        }

        ViewBag.Categories = ResourceCategories.All;
        ViewBag.SelectedCategory = category;
        ViewBag.Search = search;

        return View(await resources
            .OrderBy(resource => resource.Category)
            .ThenBy(resource => resource.Title)
            .ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var resource = await _context.Resources
            .AsNoTracking()
            .SingleOrDefaultAsync(resource => resource.Id == id);

        return resource is null ? NotFound() : View(resource);
    }
}
