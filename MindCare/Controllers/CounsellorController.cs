using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;
using MindCare.ViewModels;
using MindCare.Services;

namespace MindCare.Controllers;

[Authorize(Roles = RoleNames.Counsellor)]
public class CounsellorController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NotificationService _notificationService;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public CounsellorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService, SignInManager<ApplicationUser> signInManager)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
        _signInManager = signInManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        await _notificationService.EnsureChatAvailableNotificationsAsync();

        return View(await GetCurrentCounsellorProfileAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var profile = await GetCurrentCounsellorProfileAsync();
        if (profile is null)
        {
            return Challenge();
        }

        return View(profile);
    }

    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var profile = await GetCurrentCounsellorProfileAsync();
        if (profile is null)
        {
            return Challenge();
        }

        return View(new EditCounsellorProfileViewModel
        {
            Name = profile.ApplicationUser.Name,
            Email = profile.ApplicationUser.Email ?? string.Empty,
            Phone = profile.Phone,
            Specialization = profile.Specialization,
            Qualification = profile.Qualification,
            Experience = profile.Experience
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditCounsellorProfileViewModel model)
    {
        var profile = await GetCurrentCounsellorProfileAsync();
        if (profile is null)
        {
            return Challenge();
        }

        var email = model.Email.Trim();
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null && existingUser.Id != profile.ApplicationUserId)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = profile.ApplicationUser;
        user.Name = model.Name.Trim();
        user.Email = email;
        user.UserName = email;
        profile.Phone = model.Phone.Trim();
        profile.Specialization = model.Specialization.Trim();
        profile.Qualification = model.Qualification.Trim();
        profile.Experience = model.Experience.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult ChangeInitialPassword() => View("ChangeInitialPassword", new CounsellorChangePasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeInitialPassword(CounsellorChangePasswordViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!ModelState.IsValid) return View(model);

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        var claimResult = await RemovePasswordChangeRequirementAsync(user);
        if (!claimResult.Succeeded)
        {
            AddIdentityErrors(claimResult);
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["SuccessMessage"] = "Password changed successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult ChangePassword() => View(new CounsellorChangePasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(CounsellorChangePasswordViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!ModelState.IsValid) return View(model);

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["SuccessMessage"] = "Password changed successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public async Task<IActionResult> Availability()
    {
        var profile = await GetCurrentCounsellorProfileAsync();
        if (profile is null)
        {
            return Challenge();
        }

        return View(new CounsellorAvailabilityViewModel
        {
            Slots = await GetSlotsAsync(profile.Id)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Availability(CounsellorAvailabilityViewModel model)
    {
        var profile = await GetCurrentCounsellorProfileAsync();
        if (profile is null)
        {
            return Challenge();
        }

        var form = model.Form;
        var date = form.Date.Date;
        var now = DateTime.Now;

        if (date < now.Date)
        {
            ModelState.AddModelError("Form.Date", "Availability cannot be created in the past.");
        }

        if (date == now.Date && form.StartTime <= now.TimeOfDay)
        {
            ModelState.AddModelError("Form.StartTime", "Start time must be in the future.");
        }

        if (form.StartTime >= form.EndTime)
        {
            ModelState.AddModelError("Form.StartTime", "Start time must be before end time.");
        }

        if (form.SlotDurationMinutes <= 0)
        {
            ModelState.AddModelError("Form.SlotDurationMinutes", "Slot duration must be greater than 0.");
        }

        var totalMinutes = (int)(form.EndTime - form.StartTime).TotalMinutes;
        if (totalMinutes > 0 && form.SlotDurationMinutes > 0 && totalMinutes % form.SlotDurationMinutes != 0)
        {
            ModelState.AddModelError("Form.SlotDurationMinutes", "The availability period must be divisible by the slot duration.");
        }

        var overlapsExistingSlot = await _context.AvailabilitySlots.AnyAsync(slot =>
            slot.CounsellorProfileId == profile.Id &&
            slot.Date == date &&
            form.StartTime < slot.EndTime &&
            form.EndTime > slot.StartTime);

        if (overlapsExistingSlot)
        {
            ModelState.AddModelError(string.Empty, "This availability overlaps with an existing slot.");
        }

        if (!ModelState.IsValid)
        {
            model.Slots = await GetSlotsAsync(profile.Id);
            return View(model);
        }

        var slots = new List<AvailabilitySlot>();
        var currentStart = form.StartTime;
        var duration = TimeSpan.FromMinutes(form.SlotDurationMinutes);

        while (currentStart + duration <= form.EndTime)
        {
            slots.Add(new AvailabilitySlot
            {
                CounsellorProfileId = profile.Id,
                Date = date,
                StartTime = currentStart,
                EndTime = currentStart + duration
            });

            currentStart += duration;
        }

        _context.AvailabilitySlots.AddRange(slots);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"{slots.Count} availability slot(s) created.";
        return RedirectToAction(nameof(Availability));
    }

    [HttpGet]
    public async Task<IActionResult> BookedAppointments(int? appointmentId)
    {
        var profile = await GetCurrentCounsellorProfileAsync();
        if (profile is null)
        {
            return Challenge();
        }

        await _notificationService.EnsureChatAvailableNotificationsAsync();

        var today = DateTime.Today;
        var now = DateTime.Now.TimeOfDay;

        var appointments = await _context.Appointments
            .Include(appointment => appointment.User)
            .Where(appointment => appointment.CounsellorProfileId == profile.Id && appointment.Date >= today)
            .OrderBy(appointment => appointment.Date == today && appointment.StartTime <= now && now < appointment.EndTime ? 0 : appointment.Date > today || (appointment.Date == today && appointment.StartTime > now) ? 1 : 2)
            .ThenBy(appointment => appointment.Date)
            .ThenBy(appointment => appointment.StartTime)
            .ToListAsync();

        if (appointmentId.HasValue && appointments.Any(appointment => appointment.Id == appointmentId.Value))
        {
            ViewData["FocusedAppointmentId"] = appointmentId.Value;
        }

        return View(appointments);
    }

    private async Task<CounsellorProfile?> GetCurrentCounsellorProfileAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return null;
        }

        return await _context.CounsellorProfiles
            .Include(profile => profile.ApplicationUser)
            .FirstOrDefaultAsync(profile => profile.ApplicationUserId == user.Id);
    }

    private async Task<List<AvailabilitySlot>> GetSlotsAsync(int counsellorProfileId)
    {
        var today = DateTime.Today;
        return await _context.AvailabilitySlots
            .Include(slot => slot.Appointment)
            .Where(slot => slot.CounsellorProfileId == counsellorProfileId && slot.Date >= today)
            .OrderBy(slot => slot.Date)
            .ThenBy(slot => slot.StartTime)
            .ToListAsync();
    }

    private async Task<IdentityResult> RemovePasswordChangeRequirementAsync(ApplicationUser user)
    {
        var claims = await _userManager.GetClaimsAsync(user);
        foreach (var claim in claims.Where(claim => claim.Type == SecurityClaimTypes.MustChangePassword))
        {
            var result = await _userManager.RemoveClaimAsync(user, claim);
            if (!result.Succeeded) return result;
        }

        return IdentityResult.Success;
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
    }
}
