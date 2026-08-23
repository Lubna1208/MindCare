using System.ComponentModel.DataAnnotations;

namespace MindCare.ViewModels;

public class CreateAvailabilityViewModel
{
    [Required]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "Start Time")]
    public TimeSpan StartTime { get; set; }

    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "End Time")]
    public TimeSpan EndTime { get; set; }

    [Required]
    [Range(1, 1440, ErrorMessage = "Slot duration must be greater than 0.")]
    [Display(Name = "Slot Duration (minutes)")]
    public int SlotDurationMinutes { get; set; } = 60;
}
