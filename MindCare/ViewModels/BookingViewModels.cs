using MindCare.Models;

namespace MindCare.ViewModels;

public class BookAppointmentViewModel
{
    public bool IsMatchMode { get; set; }

    public CounsellorMatchPageViewModel? Matching { get; set; }

    public int? CounsellorProfileId { get; set; }

    public DateTime? Date { get; set; }

    public IReadOnlyList<CounsellorProfile> Counsellors { get; set; } = [];

    public IReadOnlyList<AvailabilitySlot> AvailableSlots { get; set; } = [];
}

public class PaymentSummaryViewModel
{
    public int SlotId { get; set; }

    public AvailabilitySlot Slot { get; set; } = null!;

    public long AmountMinorUnits { get; set; }

    public string Currency { get; set; } = "bdt";
}
