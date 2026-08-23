using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;
using MindCare.ViewModels;

namespace MindCare.Controllers;

[Authorize(Roles = RoleNames.User)]
public class BookingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public BookingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? counsellorProfileId, DateTime? date)
    {
        return View(await BuildBookingViewModelAsync(counsellorProfileId, date));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectSlot(int slotId)
    {
        var slot = await GetAvailableSlotAsync(slotId);
        if (slot is null)
        {
            TempData["ErrorMessage"] = "That slot is no longer available.";
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(DummyPayment), new { slotId });
    }

    [HttpGet]
    public async Task<IActionResult> DummyPayment(int slotId)
    {
        var slot = await GetAvailableSlotAsync(slotId);
        if (slot is null)
        {
            TempData["ErrorMessage"] = "That slot is no longer available.";
            return RedirectToAction(nameof(Index));
        }

        return View(new DummyPaymentViewModel
        {
            SlotId = slot.Id,
            Slot = slot
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmDummyPayment(int slotId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null || !await _userManager.IsInRoleAsync(user, RoleNames.User))
        {
            return Challenge();
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var slot = await _context.AvailabilitySlots
                .Include(item => item.CounsellorProfile)
                .ThenInclude(profile => profile.ApplicationUser)
                .Include(item => item.Appointment)
                .FirstOrDefaultAsync(item => item.Id == slotId);

            if (slot is null)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "That slot is no longer available.";
                return RedirectToAction(nameof(Index));
            }

            if (slot.CounsellorProfile is null || slot.CounsellorProfile.ApplicationUser is null)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "The selected counsellor could not be verified.";
                return RedirectToAction(nameof(Index));
            }

            var alreadyBooked = slot.IsBooked ||
                slot.Appointment is not null ||
                await _context.Appointments.AnyAsync(appointment => appointment.AvailabilitySlotId == slot.Id);

            if (alreadyBooked)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Sorry, this slot has already been booked.";
                return RedirectToAction(nameof(Index));
            }

            if (!IsSlotInFuture(slot))
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "That slot is in the past and can no longer be booked.";
                return RedirectToAction(nameof(Index));
            }

            slot.IsBooked = true;
            var appointment = new Appointment
            {
                UserId = user.Id,
                CounsellorProfileId = slot.CounsellorProfileId,
                AvailabilitySlotId = slot.Id,
                Date = slot.Date,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                Status = AppointmentStatuses.Booked,
                PaymentStatus = PaymentStatuses.DummyPaid,
                CreatedAt = DateTime.Now
            };

            _context.Appointments.Add(appointment);

            try
            {
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Sorry, this slot has already been booked.";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Confirmation), new { id = appointment.Id });
        });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var appointment = await _context.Appointments
            .Include(item => item.CounsellorProfile)
            .ThenInclude(profile => profile.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == user.Id);

        if (appointment is null)
        {
            return NotFound();
        }

        return View(appointment);
    }

    [HttpGet]
    public async Task<IActionResult> MyAppointments()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var today = DateTime.Today;
        var now = DateTime.Now.TimeOfDay;

        var appointments = await _context.Appointments
            .Include(appointment => appointment.CounsellorProfile)
            .ThenInclude(profile => profile.ApplicationUser)
            .Where(appointment => appointment.UserId == user.Id)
            .OrderBy(appointment => appointment.Date < today || (appointment.Date == today && appointment.StartTime < now))
            .ThenBy(appointment => appointment.Date)
            .ThenBy(appointment => appointment.StartTime)
            .ToListAsync();

        return View(appointments);
    }

    private async Task<BookAppointmentViewModel> BuildBookingViewModelAsync(int? counsellorProfileId, DateTime? date)
    {
        var selectedDate = date?.Date;
        var counsellors = await _context.CounsellorProfiles
            .Include(profile => profile.ApplicationUser)
            .OrderBy(profile => profile.ApplicationUser.Name)
            .ToListAsync();

        var availableSlots = new List<AvailabilitySlot>();
        if (counsellorProfileId.HasValue && selectedDate.HasValue && selectedDate.Value >= DateTime.Today)
        {
            availableSlots = await _context.AvailabilitySlots
                .Include(slot => slot.CounsellorProfile)
                .ThenInclude(profile => profile.ApplicationUser)
                .Where(slot =>
                    slot.CounsellorProfileId == counsellorProfileId.Value &&
                    slot.Date == selectedDate.Value &&
                    !slot.IsBooked)
                .OrderBy(slot => slot.StartTime)
                .ToListAsync();

            availableSlots = availableSlots
                .Where(IsSlotBookable)
                .ToList();
        }

        return new BookAppointmentViewModel
        {
            CounsellorProfileId = counsellorProfileId,
            Date = selectedDate,
            Counsellors = counsellors,
            AvailableSlots = availableSlots
        };
    }

    private async Task<AvailabilitySlot?> GetAvailableSlotAsync(int slotId)
    {
        var slot = await _context.AvailabilitySlots
            .Include(item => item.CounsellorProfile)
            .ThenInclude(profile => profile.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == slotId);

        return slot is not null && IsSlotBookable(slot) ? slot : null;
    }

    private static bool IsSlotBookable(AvailabilitySlot slot)
    {
        var today = DateTime.Today;
        var now = DateTime.Now.TimeOfDay;

        return !slot.IsBooked && IsSlotInFuture(slot);
    }

    private static bool IsSlotInFuture(AvailabilitySlot slot)
    {
        var today = DateTime.Today;
        var now = DateTime.Now.TimeOfDay;

        return slot.Date > today || (slot.Date == today && slot.StartTime > now);
    }
}
