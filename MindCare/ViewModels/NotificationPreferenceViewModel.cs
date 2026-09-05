using System.ComponentModel.DataAnnotations;

namespace MindCare.ViewModels;

public class NotificationPreferenceViewModel
{
    public bool DailyMoodReminderEnabled { get; set; }
    [Required, Display(Name = "Reminder Time")]
    public TimeOnly DailyMoodReminderTime { get; set; } = new(20, 0);
}
