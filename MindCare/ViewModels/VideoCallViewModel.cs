namespace MindCare.ViewModels;

public sealed class VideoCallViewModel
{
    public int AppointmentId { get; init; }
    public string RoomName { get; init; } = string.Empty;
    public string Domain { get; init; } = "meet.jit.si";
    public string CurrentParticipantDisplayName { get; init; } = string.Empty;
    public string OtherParticipantDisplayName { get; init; } = string.Empty;
    public DateTime AppointmentDate { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public DateTime ServerNow { get; init; }
    public DateTime AppointmentEndTime { get; init; }
    public string ReturnController { get; init; } = "Booking";
    public string ReturnAction { get; init; } = "MyAppointments";
    public string? ErrorMessage { get; init; }
    public bool CanJoin => string.IsNullOrEmpty(ErrorMessage);
}
