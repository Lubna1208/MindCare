using System.ComponentModel.DataAnnotations;

namespace MindCare.Models;

public class AvailabilitySlot
{
    public int Id { get; set; }

    public int CounsellorProfileId { get; set; }

    public CounsellorProfile CounsellorProfile { get; set; } = null!;

    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public bool IsBooked { get; set; }

    public Appointment? Appointment { get; set; }
}
