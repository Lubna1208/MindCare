using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;
using MindCare.ViewModels;

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

        if (profile is not null)
        {
            var highPriorityCount = await _context.Appointments
                .CountAsync(appointment =>
                    appointment.CounsellorProfileId == profile.Id &&
                    (appointment.IsHighRisk || appointment.Priority == AppointmentPriorities.High) &&
                    (appointment.Status == AppointmentStatuses.Booked || appointment.Status == AppointmentStatuses.Confirmed));

            ViewBag.HighPriorityAppointmentsCount = highPriorityCount;
        }

        return View(profile);
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
    public async Task<IActionResult> BookedAppointments()
    {
        var profile = await GetCurrentCounsellorProfileAsync();
        if (profile is null)
        {
            return Challenge();
        }

        var today = DateTime.Today;
        var now = DateTime.Now.TimeOfDay;

        var appointments = await _context.Appointments
            .Include(appointment => appointment.User)
            .Where(appointment => appointment.CounsellorProfileId == profile.Id)
            .OrderBy(appointment => appointment.Date < today || (appointment.Date == today && appointment.StartTime < now))
            .ThenBy(appointment => appointment.Date)
            .ThenBy(appointment => appointment.StartTime)
            .ToListAsync();

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
            .FirstOrDefaultAsync(profile => profile.ApplicationUserId == user.Id);
    }

    private async Task<List<AvailabilitySlot>> GetSlotsAsync(int counsellorProfileId)
    {
        return await _context.AvailabilitySlots
            .Include(slot => slot.Appointment)
            .Where(slot => slot.CounsellorProfileId == counsellorProfileId)
            .OrderBy(slot => slot.Date)
            .ThenBy(slot => slot.StartTime)
            .ToListAsync();
    }
}
