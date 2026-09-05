using MindCare.Models;

namespace MindCare.ViewModels;

public class AppointmentChatViewModel
{
    public Appointment? Appointment { get; set; }

    public IReadOnlyList<Message> Messages { get; set; } = new List<Message>();

    public string CurrentUserId { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public string? ReturnController { get; set; }

    public string? ReturnAction { get; set; }
}
