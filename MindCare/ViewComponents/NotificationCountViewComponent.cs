using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;

namespace MindCare.ViewComponents;

public class NotificationCountViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user is null) return Content(string.Empty);
        var unreadCount = await context.Notifications.CountAsync(item => item.UserId == user.Id && !item.IsRead);
        return View(unreadCount);
    }
}
