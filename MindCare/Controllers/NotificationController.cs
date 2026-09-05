using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;
using MindCare.Services;
using MindCare.ViewModels;

namespace MindCare.Controllers;

[Authorize(Roles = RoleNames.User + "," + RoleNames.Counsellor)]
public class NotificationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NotificationService _notificationService;

    public NotificationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
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

        await _notificationService.EnsureChatAvailableNotificationsAsync();
        if (User.IsInRole(RoleNames.User)) await _notificationService.EnsureDailyMoodReminderAsync(user.Id);

        var notifications = await _context.Notifications.Where(item => item.UserId == user.Id)
            .OrderByDescending(item => item.CreatedAt).ToListAsync();
        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var notification = await _context.Notifications.FirstOrDefaultAsync(item => item.Id == id && item.UserId == user.Id);
        if (notification is null) return NotFound();

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.User)]
    [HttpGet]
    public async Task<IActionResult> Settings()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var preference = await _context.NotificationPreferences.SingleOrDefaultAsync(item => item.UserId == user.Id);
        return View(new NotificationPreferenceViewModel
        {
            DailyMoodReminderEnabled = preference?.DailyMoodReminderEnabled ?? false,
            DailyMoodReminderTime = preference?.DailyMoodReminderTime ?? new TimeOnly(20, 0)
        });
    }

    [Authorize(Roles = RoleNames.User)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(NotificationPreferenceViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var preference = await _context.NotificationPreferences.SingleOrDefaultAsync(item => item.UserId == user.Id);
        if (preference is null)
        {
            preference = new NotificationPreference { UserId = user.Id };
            _context.NotificationPreferences.Add(preference);
        }
        preference.DailyMoodReminderEnabled = model.DailyMoodReminderEnabled;
        preference.DailyMoodReminderTime = model.DailyMoodReminderTime;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Notification settings saved.";
        return RedirectToAction(nameof(Settings));
    }
}
