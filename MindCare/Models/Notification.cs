using System.ComponentModel.DataAnnotations;

namespace MindCare.Models;

public class Notification
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    [Required, StringLength(500)] public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    [StringLength(50)] public string? NotificationType { get; set; }
    public int? AppointmentId { get; set; }
    [StringLength(120)] public string? EventKey { get; set; }
}

public static class NotificationTypes
{
    public const string AppointmentBooked = "AppointmentBooked";
    public const string NewAppointment = "NewAppointment";
    public const string ChatAvailable = "ChatAvailable";
    public const string DailyMoodReminder = "DailyMoodReminder";
}
