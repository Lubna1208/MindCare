using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MindCare.Models;

namespace MindCare.Services;

public class RoleRedirectService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RoleRedirectService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> RedirectToDashboardAsync(ApplicationUser user, IUrlHelper url)
    {
        if (await _userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            return new RedirectToActionResult("Index", "Admin", null);
        }

        if (await _userManager.IsInRoleAsync(user, RoleNames.Counsellor))
        {
            return new RedirectToActionResult("Index", "Counsellor", null);
        }

        return new RedirectToActionResult("Index", "Dashboard", null);
    }
}
