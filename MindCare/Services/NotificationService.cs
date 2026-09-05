using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;

namespace MindCare.Services;

public class NotificationService(ApplicationDbContext context)
{
    public async Task CreateAppointmentBookedNotificationsAsync(int appointmentId)
    {
        var appointment = await context.Appointments.Include(item => item.CounsellorProfile).ThenInclude(profile => profile.ApplicationUser).FirstOrDefaultAsync(item => item.Id == appointmentId);
        if (appointment is null) return;
        await AddIfMissingAsync(appointment.UserId, "Your appointment has been successfully booked.", NotificationTypes.AppointmentBooked, appointment.Id, $"AppointmentBooked:{appointment.Id}");
        await AddIfMissingAsync(appointment.CounsellorProfile.ApplicationUserId, "You have a new appointment booking.", NotificationTypes.NewAppointment, appointment.Id, $"NewAppointment:{appointment.Id}");
        await context.SaveChangesAsync();
    }

    public async Task EnsureChatAvailableNotificationsAsync()
    {
        var now = DateTime.Now;
        var appointments = await context.Appointments.Include(item => item.CounsellorProfile)
            .Where(item => (item.Status == AppointmentStatuses.Booked || item.Status == AppointmentStatuses.Confirmed) && (item.PaymentStatus == PaymentStatuses.Paid || item.PaymentStatus == PaymentStatuses.Confirmed) && item.Date <= now.Date).ToListAsync();
        var changed = false;
        foreach (var appointment in appointments.Where(item => now >= item.Date.Date.Add(item.StartTime)))
        {
            changed |= await AddIfMissingAsync(appointment.UserId, "Your appointment has started. You can now chat with your counsellor.", NotificationTypes.ChatAvailable, appointment.Id, $"ChatAvailable:{appointment.Id}");
            changed |= await AddIfMissingAsync(appointment.CounsellorProfile.ApplicationUserId, "Your appointment has started. You can now chat with the user.", NotificationTypes.ChatAvailable, appointment.Id, $"ChatAvailable:{appointment.Id}");
        }
        if (changed) await context.SaveChangesAsync();
    }

    public async Task EnsureDailyMoodReminderAsync(string userId)
    {
        var preference = await context.NotificationPreferences.SingleOrDefaultAsync(item => item.UserId == userId);
        if (preference is null || !preference.DailyMoodReminderEnabled) return;
        var now = DateTime.Now;
        if (now.TimeOfDay < preference.DailyMoodReminderTime.ToTimeSpan()) return;
        var today = now.Date;
        if (await context.MoodLogs.AnyAsync(item => item.UserId == userId && item.CreatedAt >= today && item.CreatedAt < today.AddDays(1))) return;
        if (await AddIfMissingAsync(userId, "Time for your daily mood check-in! How are you feeling today?", NotificationTypes.DailyMoodReminder, null, $"DailyMoodReminder:{today:yyyyMMdd}")) await context.SaveChangesAsync();
    }

    private async Task<bool> AddIfMissingAsync(string userId, string message, string notificationType, int? appointmentId, string eventKey)
    {
        if (await context.Notifications.AnyAsync(item => item.UserId == userId && item.EventKey == eventKey)) return false;
        context.Notifications.Add(new Notification { UserId = userId, Message = message, NotificationType = notificationType, AppointmentId = appointmentId, EventKey = eventKey, CreatedAt = DateTime.Now });
        return true;
    }
}
