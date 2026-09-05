using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;
using MindCare.ViewModels;
using Stripe;
using Stripe.Checkout;

namespace MindCare.Controllers;

[Authorize(Roles = RoleNames.User)]
public class BookingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public BookingController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
    }

    private long AppointmentFeeCents =>
        _configuration.GetValue<long?>("Stripe:AppointmentFeeCents") ?? 1500;

    private string Currency =>
        _configuration.GetValue<string?>("Stripe:Currency") ?? "usd";

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

        return RedirectToAction(nameof(PaymentSummary), new { slotId });
    }

    [HttpGet]
    public async Task<IActionResult> PaymentSummary(int slotId)
    {
        var slot = await GetAvailableSlotAsync(slotId);
        if (slot is null)
        {
            TempData["ErrorMessage"] = "That slot is no longer available.";
            return RedirectToAction(nameof(Index));
        }

        return View(new PaymentSummaryViewModel
        {
            SlotId = slot.Id,
            Slot = slot,
            AmountCents = AppointmentFeeCents,
            Currency = Currency
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCheckoutSession(int slotId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var slot = await GetAvailableSlotAsync(slotId);
        if (slot is null)
        {
            TempData["ErrorMessage"] = "That slot is no longer available.";
            return RedirectToAction(nameof(Index));
        }

        var counsellorName = slot.CounsellorProfile.ApplicationUser.Name;
        var successUrl = Url.Action(nameof(PaymentSuccess), "Booking", null, Request.Scheme)
            + "?session_id={CHECKOUT_SESSION_ID}";
        var cancelUrl = Url.Action(nameof(PaymentCancelled), "Booking", new { slotId }, Request.Scheme);

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            CustomerEmail = user.Email,
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = Currency,
                        UnitAmount = AppointmentFeeCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"MindCare Counselling Session with {counsellorName}",
                            Description = $"{slot.Date:dd MMMM yyyy}, {FormatTime(slot.StartTime)} - {FormatTime(slot.EndTime)}"
                        }
                    }
                }
            },
            Metadata = new Dictionary<string, string>
            {
                { "slotId", slot.Id.ToString() },
                { "userId", user.Id }
            }
        };

        var service = new SessionService();
        Session session;

        try
        {
            session = await service.CreateAsync(options);
        }
        catch (StripeException)
        {
            TempData["ErrorMessage"] = "We could not start the payment process. Please try again.";
            return RedirectToAction(nameof(PaymentSummary), new { slotId });
        }

        return Redirect(session.Url);
    }

    [HttpGet]
    public async Task<IActionResult> PaymentSuccess(string session_id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (string.IsNullOrWhiteSpace(session_id))
        {
            TempData["ErrorMessage"] = "We could not verify your payment.";
            return RedirectToAction(nameof(Index));
        }

        var sessionService = new SessionService();
        Session session;

        try
        {
            session = await sessionService.GetAsync(session_id);
        }
        catch (StripeException)
        {
            TempData["ErrorMessage"] = "We could not verify your payment. Please contact support if you were charged.";
            return RedirectToAction(nameof(Index));
        }

        if (session.PaymentStatus != "paid")
        {
            TempData["ErrorMessage"] = "Your payment was not completed.";
            return RedirectToAction(nameof(Index));
        }

        if (!session.Metadata.TryGetValue("slotId", out var slotIdText) ||
            !int.TryParse(slotIdText, out var slotId))
        {
            TempData["ErrorMessage"] = "We could not verify your payment details.";
            return RedirectToAction(nameof(Index));
        }

        var existingAppointment = await _context.Appointments
            .FirstOrDefaultAsync(appointment => appointment.StripeSessionId == session.Id);

        if (existingAppointment is not null)
        {
            return RedirectToAction(nameof(Confirmation), new { id = existingAppointment.Id });
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        var appointmentId = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var slot = await _context.AvailabilitySlots
                .Include(item => item.CounsellorProfile)
                .ThenInclude(profile => profile.ApplicationUser)
                .Include(item => item.Appointment)
                .FirstOrDefaultAsync(item => item.Id == slotId);

            if (slot is null || slot.CounsellorProfile?.ApplicationUser is null)
            {
                await transaction.RollbackAsync();
                return (int?)null;
            }

            var alreadyBooked = slot.IsBooked ||
                slot.Appointment is not null ||
                await _context.Appointments.AnyAsync(appointment => appointment.AvailabilitySlotId == slot.Id);

            if (alreadyBooked)
            {
                await transaction.RollbackAsync();
                return (int?)null;
            }

            var latestAssessment = await _context.Assessments
                .Where(assessmentItem => assessmentItem.UserId == user.Id)
                .OrderByDescending(assessmentItem => assessmentItem.CreatedAt)
                .ThenByDescending(assessmentItem => assessmentItem.Id)
                .FirstOrDefaultAsync();

            var isHighRisk = string.Equals(latestAssessment?.RiskLevel?.Trim(), "High", StringComparison.OrdinalIgnoreCase);

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
                PaymentStatus = PaymentStatuses.Paid,
                IsHighRisk = isHighRisk,
                RiskLevel = isHighRisk ? "High" : (latestAssessment?.RiskLevel ?? "Low"),
                Priority = isHighRisk ? AppointmentPriorities.High : AppointmentPriorities.Normal,
                StripeSessionId = session.Id,
                StripePaymentIntentId = session.PaymentIntentId,
                AmountPaidCents = session.AmountTotal,
                Currency = session.Currency,
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
                return (int?)null;
            }

            return appointment.Id;
        });

        if (appointmentId is null)
        {
            TempData["ErrorMessage"] =
                "Your payment succeeded, but this slot was already booked. Please contact support with your payment reference: " + session.Id;
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Confirmation), new { id = appointmentId });
    }

    [HttpGet]
    public IActionResult PaymentCancelled(int slotId)
    {
        TempData["ErrorMessage"] = "Payment was cancelled. Your slot has not been booked.";
        return RedirectToAction(nameof(PaymentSummary), new { slotId });
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
        return !slot.IsBooked && IsSlotInFuture(slot);
    }

    private static bool IsSlotInFuture(AvailabilitySlot slot)
    {
        var today = DateTime.Today;
        var now = DateTime.Now.TimeOfDay;

        return slot.Date > today || (slot.Date == today && slot.StartTime > now);
    }

    private static string FormatTime(TimeSpan time) => DateTime.Today.Add(time).ToString("h:mm tt");
}
