using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    public bool IsHighRisk { get; set; }

    [StringLength(20)]
    public string? RiskLevel { get; set; }

    [Required]
    [StringLength(20)]
    public string Priority { get; set; } = AppointmentPriorities.Normal;

    [NotMapped]
    public bool IsHighPriority
    {
        get => IsHighRisk ||
               string.Equals(Priority, AppointmentPriorities.High, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(RiskLevel, "High", StringComparison.OrdinalIgnoreCase);
        set
        {
            IsHighRisk = value;
            if (value)
            {
                Priority = AppointmentPriorities.High;
                RiskLevel = "High";
            }
        }
    }

    [StringLength(255)]
    public string? StripeSessionId { get; set; }

    [StringLength(255)]
    public string? StripePaymentIntentId { get; set; }

    public long? AmountPaidCents { get; set; }

    [StringLength(10)]
    public string? Currency { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public static class AppointmentPriorities
{
    public const string Normal = "Normal";
    public const string High = "High";
}

public static class AppointmentStatuses
{
    public const string Booked = "Booked";
    public const string Confirmed = "Confirmed";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}

public static class PaymentStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Confirmed = "Confirmed";
    public const string Failed = "Failed";
}
