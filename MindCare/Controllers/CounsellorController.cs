using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;

namespace MindCare.Controllers;

[Authorize(Roles = RoleNames.Counsellor)]
public class CounsellorController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CounsellorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var profile = await _context.CounsellorProfiles
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.ApplicationUserId == user.Id);

        return View(profile);
    }
}
