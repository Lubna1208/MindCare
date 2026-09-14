using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;
using MindCare.Services;
using MindCare.ViewModels;

namespace MindCare.Controllers;

[Authorize(Roles = RoleNames.User + "," + RoleNames.Counsellor)]
public sealed class AppointmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAppointmentChatWindowService _sessionWindow;
    private readonly IVideoCallRoomService _videoRooms;

    public AppointmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        IAppointmentChatWindowService sessionWindow, IVideoCallRoomService videoRooms)
    {
        _context = context;
        _userManager = userManager;
        _sessionWindow = sessionWindow;
        _videoRooms = videoRooms;
    }

    [HttpGet("Appointments/VideoCall/{appointmentId:int}")]
    public async Task<IActionResult> VideoCall(int appointmentId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var returnTarget = GetReturnTarget();
        if (currentUser is null)
        {
            return Challenge();
        }

        var appointment = await _context.Appointments
            .Include(item => item.User)
            .Include(item => item.CounsellorProfile)
            .ThenInclude(profile => profile.ApplicationUser)
            .FirstOrDefaultAsync(item => item.Id == appointmentId);

        if (appointment is null || !IsParticipant(appointment, currentUser.Id))
        {
            return Unavailable("This video session is unavailable.", returnTarget);
        }

        if (!IsPaidOrConfirmed(appointment) || !IsBookedOrConfirmed(appointment))
        {
            return Unavailable("Video is unavailable for this appointment.", returnTarget);
        }

        if (_sessionWindow.GetState(appointment) != AppointmentChatWindowState.Open)
        {
            return Unavailable("Video is available only while the appointment session is active.", returnTarget);
        }

        if (!_videoRooms.IsConfigured)
        {
            return Unavailable("Video calling is temporarily unavailable. You can continue using appointment chat.", returnTarget);
        }

        var isClient = appointment.UserId == currentUser.Id;
        return View(new VideoCallViewModel
        {
            AppointmentId = appointment.Id,
            RoomName = _videoRooms.GetRoomName(appointment.Id),
            Domain = _videoRooms.Domain,
            CurrentParticipantDisplayName = currentUser.Name,
            OtherParticipantDisplayName = isClient ? appointment.CounsellorProfile.ApplicationUser.Name : appointment.User.Name,
            AppointmentDate = appointment.Date.Date,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            ServerNow = _sessionWindow.GetCurrentLocalTime(),
            AppointmentEndTime = appointment.Date.Date.Add(appointment.EndTime),
            ReturnController = returnTarget.Controller,
            ReturnAction = returnTarget.Action
        });
    }

    private IActionResult Unavailable(string message, (string Controller, string Action) returnTarget) =>
        View("VideoCall", new VideoCallViewModel { ErrorMessage = message, ReturnController = returnTarget.Controller, ReturnAction = returnTarget.Action });

    private static bool IsParticipant(Appointment appointment, string userId) =>
        appointment.UserId == userId || appointment.CounsellorProfile.ApplicationUserId == userId;

    private static bool IsPaidOrConfirmed(Appointment appointment) =>
        appointment.PaymentStatus is PaymentStatuses.Paid or PaymentStatuses.Confirmed;

    private static bool IsBookedOrConfirmed(Appointment appointment) =>
        appointment.Status is AppointmentStatuses.Booked or AppointmentStatuses.Confirmed;

    private (string Controller, string Action) GetReturnTarget() =>
        User.IsInRole(RoleNames.Counsellor) ? ("Counsellor", "BookedAppointments") : ("Booking", "MyAppointments");
}
