using MindCare.Models;

namespace MindCare.ViewModels;

public class BookAppointmentViewModel
{
    public int? CounsellorProfileId { get; set; }

    public DateTime? Date { get; set; }

    public IReadOnlyList<CounsellorProfile> Counsellors { get; set; } = [];

    public IReadOnlyList<AvailabilitySlot> AvailableSlots { get; set; } = [];
}

public class DummyPaymentViewModel
{
    public int SlotId { get; set; }

    public AvailabilitySlot Slot { get; set; } = null!;
}
