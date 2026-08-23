using System.ComponentModel.DataAnnotations;

namespace MindCare.Models;

public class Appointment
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int CounsellorProfileId { get; set; }

    public CounsellorProfile CounsellorProfile { get; set; } = null!;

    public int AvailabilitySlotId { get; set; }

    public AvailabilitySlot AvailabilitySlot { get; set; } = null!;

    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = AppointmentStatuses.Booked;

    [Required]
    [StringLength(20)]
    public string PaymentStatus { get; set; } = PaymentStatuses.Pending;

    public DateTime CreatedAt { get; set; }
}

public static class AppointmentStatuses
{
    public const string Booked = "Booked";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}

public static class PaymentStatuses
{
    public const string Pending = "Pending";
    public const string DummyPaid = "DummyPaid";
}
