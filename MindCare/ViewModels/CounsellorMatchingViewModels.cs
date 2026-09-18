using System.ComponentModel.DataAnnotations;

namespace MindCare.ViewModels;

public enum CounsellorConcern
{
    [Display(Name = "Stress")]
    Stress,
    [Display(Name = "Anxiety")]
    Anxiety,
    [Display(Name = "Study Pressure")]
    StudyPressure,
    [Display(Name = "Relationship Concerns")]
    RelationshipConcerns,
    [Display(Name = "General Wellbeing")]
    GeneralWellbeing
}

public sealed class CounsellorMatchRequestViewModel
{
    [Required(ErrorMessage = "Choose an area of support to find counsellors.")]
    public CounsellorConcern? Concern { get; set; }
}

public sealed class CounsellorMatchResultViewModel
{
    public int CounsellorProfileId { get; init; }
    public string CounsellorName { get; init; } = string.Empty;
    public string Specialization { get; init; } = string.Empty;
    public DateTime NextAvailableDate { get; init; }
    public TimeSpan NextAvailableStartTime { get; init; }
    public string Explanation { get; set; } = string.Empty;
    public bool HasAiExplanation { get; set; }
}

public sealed class CounsellorMatchPageViewModel
{
    public CounsellorMatchRequestViewModel Request { get; init; } = new();
    public IReadOnlyList<CounsellorMatchResultViewModel>? Matches { get; init; }
}
