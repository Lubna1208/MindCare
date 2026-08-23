using MindCare.Models;

namespace MindCare.ViewModels;

public class CounsellorAvailabilityViewModel
{
    public CreateAvailabilityViewModel Form { get; set; } = new();

    public IReadOnlyList<AvailabilitySlot> Slots { get; set; } = [];
}
