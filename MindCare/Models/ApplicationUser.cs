using Microsoft.AspNetCore.Identity;

namespace MindCare.Models;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;

    public ICollection<MoodLog> MoodLogs { get; set; } = new List<MoodLog>();

    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public ICollection<Message> SentMessages { get; set; } = new List<Message>();

    public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
}
