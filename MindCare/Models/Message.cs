using System.ComponentModel.DataAnnotations;

namespace MindCare.Models;

public class Message
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    public Appointment Appointment { get; set; } = null!;

    [Required]
    public string SenderUserId { get; set; } = string.Empty;

    public ApplicationUser SenderUser { get; set; } = null!;

    [Required]
    public string ReceiverUserId { get; set; } = string.Empty;

    public ApplicationUser ReceiverUser { get; set; } = null!;

    [Required]
    [StringLength(2000)]
    public string MessageText { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }
}
