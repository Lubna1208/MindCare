using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;
using MindCare.ViewModels;

namespace MindCare.Controllers;

[Authorize(Roles = RoleNames.User + "," + RoleNames.Counsellor)]
public class MessagingController : Controller
{
    private const int MaxMessageLength = 2000;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MessagingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Chat(int appointmentId)
    {
        var access = await GetChatAccessAsync(appointmentId);
        if (!access.CanOpenChat)
        {
            return View(BuildErrorViewModel(access));
        }

        var appointment = access.Appointment!;
        appointment.Messages = await _context.Messages
            .Include(message => message.SenderUser)
            .Where(message => message.AppointmentId == appointment.Id)
            .OrderBy(message => message.SentAt)
            .ToListAsync();

        return View(new AppointmentChatViewModel
        {
            Appointment = appointment,
            Messages = appointment.Messages.OrderBy(message => message.SentAt).ToList(),
            CurrentUserId = access.CurrentUserId,
            ReturnController = access.ReturnController,
            ReturnAction = access.ReturnAction
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int appointmentId, string? messageText)
    {
        var access = await GetChatAccessAsync(appointmentId);
        if (!access.CanOpenChat)
        {
            TempData["ErrorMessage"] = access.ErrorMessage;
            return RedirectToAction(nameof(Chat), new { appointmentId });
        }

        var trimmedMessage = messageText?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedMessage))
        {
            TempData["ErrorMessage"] = "Please enter a message before sending.";
            return RedirectToAction(nameof(Chat), new { appointmentId });
        }

        if (trimmedMessage.Length > MaxMessageLength)
        {
            TempData["ErrorMessage"] = $"Messages cannot be longer than {MaxMessageLength} characters.";
            return RedirectToAction(nameof(Chat), new { appointmentId });
        }

        var appointment = access.Appointment!;
        var receiverUserId = appointment.UserId == access.CurrentUserId
            ? appointment.CounsellorProfile.ApplicationUserId
            : appointment.UserId;

        _context.Messages.Add(new Message
        {
            AppointmentId = appointment.Id,
            SenderUserId = access.CurrentUserId,
            ReceiverUserId = receiverUserId,
            MessageText = trimmedMessage,
            SentAt = DateTime.Now
        });

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Chat), new { appointmentId });
    }

    private async Task<ChatAccessResult> GetChatAccessAsync(int appointmentId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return ChatAccessResult.Denied("Please sign in to open appointment messages.", string.Empty, "Account", "Login");
        }

        var appointment = await _context.Appointments
            .Include(item => item.User)
            .Include(item => item.CounsellorProfile)
            .ThenInclude(profile => profile.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == appointmentId);

        var returnTarget = GetReturnTarget();
        if (appointment is null)
        {
            return ChatAccessResult.Denied("Appointment was not found.", currentUser.Id, returnTarget.Controller, returnTarget.Action);
        }

        if (!IsParticipant(appointment, currentUser.Id))
        {
            return ChatAccessResult.Denied("You are not allowed to access this appointment conversation.", currentUser.Id, returnTarget.Controller, returnTarget.Action);
        }

        if (!IsPaidOrConfirmed(appointment))
        {
            return ChatAccessResult.Denied("Messaging is available only after the appointment payment is confirmed.", currentUser.Id, returnTarget.Controller, returnTarget.Action, appointment);
        }

        if (!IsBookedOrConfirmed(appointment))
        {
            return ChatAccessResult.Denied("Messaging is not available for cancelled or completed appointments.", currentUser.Id, returnTarget.Controller, returnTarget.Action, appointment);
        }

        if (!HasAppointmentStarted(appointment))
        {
            return ChatAccessResult.Denied("Messaging will be available when your appointment starts.", currentUser.Id, returnTarget.Controller, returnTarget.Action, appointment);
        }

        return ChatAccessResult.Allowed(currentUser.Id, returnTarget.Controller, returnTarget.Action, appointment);
    }

    private bool IsParticipant(Appointment appointment, string userId)
    {
        return appointment.UserId == userId ||
            appointment.CounsellorProfile.ApplicationUserId == userId;
    }

    private bool IsPaidOrConfirmed(Appointment appointment)
    {
        return appointment.PaymentStatus == PaymentStatuses.Paid ||
            appointment.PaymentStatus == PaymentStatuses.Confirmed;
    }

    private bool IsBookedOrConfirmed(Appointment appointment)
    {
        return appointment.Status == AppointmentStatuses.Booked ||
            appointment.Status == AppointmentStatuses.Confirmed;
    }

    private bool HasAppointmentStarted(Appointment appointment)
    {
        var appointmentStart = appointment.Date.Date.Add(appointment.StartTime);
        return DateTime.Now >= appointmentStart;
    }

    private (string Controller, string Action) GetReturnTarget()
    {
        if (User.IsInRole(RoleNames.Counsellor))
        {
            return ("Counsellor", "BookedAppointments");
        }

        return ("Booking", "MyAppointments");
    }

    private AppointmentChatViewModel BuildErrorViewModel(ChatAccessResult access)
    {
        return new AppointmentChatViewModel
        {
            Appointment = access.Appointment,
            CurrentUserId = access.CurrentUserId,
            ErrorMessage = access.ErrorMessage,
            ReturnController = access.ReturnController,
            ReturnAction = access.ReturnAction
        };
    }

    private sealed class ChatAccessResult
    {
        public bool CanOpenChat { get; private init; }

        public string CurrentUserId { get; private init; } = string.Empty;

        public string ErrorMessage { get; private init; } = string.Empty;

        public string ReturnController { get; private init; } = "Booking";

        public string ReturnAction { get; private init; } = "MyAppointments";

        public Appointment? Appointment { get; private init; }

        public static ChatAccessResult Allowed(string currentUserId, string returnController, string returnAction, Appointment appointment)
        {
            return new ChatAccessResult
            {
                CanOpenChat = true,
                CurrentUserId = currentUserId,
                ReturnController = returnController,
                ReturnAction = returnAction,
                Appointment = appointment
            };
        }

        public static ChatAccessResult Denied(
            string errorMessage,
            string currentUserId,
            string returnController,
            string returnAction,
            Appointment? appointment = null)
        {
            return new ChatAccessResult
            {
                CanOpenChat = false,
                ErrorMessage = errorMessage,
                CurrentUserId = currentUserId,
                ReturnController = returnController,
                ReturnAction = returnAction,
                Appointment = appointment
            };
        }
    }
}
