using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;

namespace MindCare.Services;

public class NotificationService(ApplicationDbContext context, IAppointmentChatWindowService chatWindowService)
{
    public async Task CreateAppointmentBookedNotificationsAsync(int appointmentId)
    {
        var appointment = await context.Appointments.Include(item => item.CounsellorProfile).ThenInclude(profile => profile.ApplicationUser).FirstOrDefaultAsync(item => item.Id == appointmentId);
        if (appointment is null) return;
        await AddIfMissingAsync(appointment.UserId, "Your appointment has been successfully booked.", NotificationTypes.AppointmentBooked, appointment.Id, $"AppointmentBooked:{appointment.Id}");
        await AddIfMissingAsync(appointment.CounsellorProfile.ApplicationUserId, "You have a new appointment booking.", NotificationTypes.NewAppointment, appointment.Id, $"NewAppointment:{appointment.Id}");
        await context.SaveChangesAsync();
    }

    public Task EnsureChatAvailableNotificationsAsync() => EnsureChatLifecycleNotificationsAsync();

    public async Task EnsureChatLifecycleNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var now = chatWindowService.GetCurrentLocalTime();
        var appointments = await context.Appointments.Include(item => item.CounsellorProfile)
            .Where(item => (item.Status == AppointmentStatuses.Booked || item.Status == AppointmentStatuses.Confirmed) &&
                (item.PaymentStatus == PaymentStatuses.Paid || item.PaymentStatus == PaymentStatuses.Confirmed) &&
                item.Date >= now.Date.AddDays(-1) && item.Date <= now.Date)
            .ToListAsync(cancellationToken);
        var changed = false;
        foreach (var appointment in appointments)
        {
            var state = chatWindowService.GetState(appointment, now);
            if (state == AppointmentChatWindowState.Open)
            {
                changed |= await AddIfMissingAsync(appointment.UserId, "Your appointment has started. You can now chat with your counsellor.", NotificationTypes.ChatAvailable, appointment.Id, $"ChatAvailable:{appointment.Id}");
                changed |= await AddIfMissingAsync(appointment.CounsellorProfile.ApplicationUserId, "Your appointment has started. Chat is now available.", NotificationTypes.ChatAvailable, appointment.Id, $"ChatAvailable:{appointment.Id}");
            }
            else if (state == AppointmentChatWindowState.Ended)
            {
                changed |= await AddIfMissingAsync(appointment.UserId, "Your appointment has ended. Appointment chat is now closed.", NotificationTypes.ChatEnded, appointment.Id, $"ChatEnded:{appointment.Id}");
                changed |= await AddIfMissingAsync(appointment.CounsellorProfile.ApplicationUserId, "The appointment has ended. Chat is now closed.", NotificationTypes.ChatEnded, appointment.Id, $"ChatEnded:{appointment.Id}");
            }
        }
        if (changed) await context.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureDailyMoodReminderAsync(string userId)
    {
        var preference = await context.NotificationPreferences.SingleOrDefaultAsync(item => item.UserId == userId);
        if (preference is null || !preference.DailyMoodReminderEnabled) return;
        var now = chatWindowService.GetCurrentLocalTime();
        if (now.TimeOfDay < preference.DailyMoodReminderTime.ToTimeSpan()) return;
        var today = now.Date;
        if (await context.MoodLogs.AnyAsync(item => item.UserId == userId && item.CreatedAt >= today && item.CreatedAt < today.AddDays(1))) return;
        if (await AddIfMissingAsync(userId, "Time for your daily mood check-in! How are you feeling today?", NotificationTypes.DailyMoodReminder, null, $"DailyMoodReminder:{today:yyyyMMdd}")) await context.SaveChangesAsync();
    }

    private async Task<bool> AddIfMissingAsync(string userId, string message, string notificationType, int? appointmentId, string eventKey)
    {
        if (await context.Notifications.AnyAsync(item => item.UserId == userId && item.EventKey == eventKey)) return false;
        context.Notifications.Add(new Notification { UserId = userId, Message = message, NotificationType = notificationType, AppointmentId = appointmentId, EventKey = eventKey, CreatedAt = chatWindowService.GetCurrentLocalTime() });
        return true;
    }
}
