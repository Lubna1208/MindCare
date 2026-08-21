using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;
using MindCare.ViewModels;

namespace MindCare.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult AddCounsellor()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCounsellor(AddCounsellorViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            Name = model.FullName.Trim(),
            UserName = email,
            Email = email,
            PhoneNumber = model.Phone.Trim(),
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _userManager.AddToRoleAsync(user, RoleNames.Counsellor);

        var profile = new CounsellorProfile
        {
            ApplicationUserId = user.Id,
            Phone = model.Phone.Trim(),
            Specialization = model.Specialization.Trim(),
            Qualification = model.Qualification.Trim(),
            Experience = model.Experience.Trim()
        };

        _context.CounsellorProfiles.Add(profile);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Counsellor account created.";
        return RedirectToAction(nameof(Counsellors));
    }

    public async Task<IActionResult> Counsellors()
    {
        var counsellors = await _context.CounsellorProfiles
            .Include(profile => profile.ApplicationUser)
            .OrderBy(profile => profile.ApplicationUser.Name)
            .ToListAsync();

        return View(counsellors);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCounsellor(int id)
    {
        var profile = await _context.CounsellorProfiles
            .Include(profile => profile.ApplicationUser)
            .FirstOrDefaultAsync(profile => profile.Id == id);

        if (profile is null)
        {
            TempData["ErrorMessage"] = "Counsellor was not found.";
            return RedirectToAction(nameof(Counsellors));
        }

        var user = profile.ApplicationUser;
        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            TempData["ErrorMessage"] = "Counsellor could not be deleted.";
            return RedirectToAction(nameof(Counsellors));
        }

        TempData["SuccessMessage"] = "Counsellor deleted.";
        return RedirectToAction(nameof(Counsellors));
    }
}
