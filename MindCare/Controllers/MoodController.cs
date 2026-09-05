using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;
using MindCare.ViewModels;
using MindCare.Services;

namespace MindCare.Controllers;

[Authorize(Roles = RoleNames.User)]
public class MoodController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NotificationService _notificationService;

    public MoodController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await _notificationService.EnsureDailyMoodReminderAsync(user.Id);
        return View(new MoodLogViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(MoodLogViewModel model)
    {
        if (!MoodLog.IsValidMood(model.Mood))
        {
            ModelState.AddModelError(nameof(model.Mood), "Please select a valid mood.");
        }

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var moodLog = new MoodLog
        {
            UserId = user.Id,
            Mood = model.Mood,
            Note = model.Note,
            CreatedAt = DateTime.Now
        };

        _context.MoodLogs.Add(moodLog);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your mood has been saved.";
        return RedirectToAction(nameof(History));
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var moodLogs = await _context.MoodLogs
            .Where(moodLog => moodLog.UserId == user.Id)
            .OrderByDescending(moodLog => moodLog.CreatedAt)
            .ToListAsync();

        return View(moodLogs);
    }
}
