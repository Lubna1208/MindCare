using System.ComponentModel.DataAnnotations;

namespace MindCare.Models;

public class NotificationPreference
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public bool DailyMoodReminderEnabled { get; set; }
    public TimeOnly DailyMoodReminderTime { get; set; } = new(20, 0);
}
