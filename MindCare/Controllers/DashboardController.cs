using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MindCare.Models;
using MindCare.Services;

namespace MindCare.Controllers;

[Authorize]
[Authorize(Roles = RoleNames.User)]
public class DashboardController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NotificationService _notificationService;

    public DashboardController(UserManager<ApplicationUser> userManager, NotificationService notificationService)
    {
        _userManager = userManager;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        await _notificationService.EnsureDailyMoodReminderAsync(user.Id);
        await _notificationService.EnsureChatAvailableNotificationsAsync();

        return View(user);
    }
}
