using MindCare.Models;

namespace MindCare.Services;

public enum AppointmentChatWindowState
{
    BeforeStart,
    Open,
    Ended
}

public interface IAppointmentChatWindowService
{
    DateTime GetCurrentLocalTime();

    AppointmentChatWindowState GetState(Appointment appointment);

    AppointmentChatWindowState GetState(Appointment appointment, DateTime now);

    bool IsOpen(Appointment appointment);
}

public sealed class AppointmentChatWindowService(TimeProvider timeProvider) : IAppointmentChatWindowService
{
    public DateTime GetCurrentLocalTime() => timeProvider.GetLocalNow().DateTime;

    public AppointmentChatWindowState GetState(Appointment appointment) => GetState(appointment, GetCurrentLocalTime());

    public AppointmentChatWindowState GetState(Appointment appointment, DateTime now)
    {
        var start = appointment.Date.Date.Add(appointment.StartTime);
        var end = appointment.Date.Date.Add(appointment.EndTime);

        if (now < start) return AppointmentChatWindowState.BeforeStart;
        return now < end ? AppointmentChatWindowState.Open : AppointmentChatWindowState.Ended;
    }

    public bool IsOpen(Appointment appointment) => GetState(appointment) == AppointmentChatWindowState.Open;
}
