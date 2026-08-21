using System.ComponentModel.DataAnnotations;

namespace MindCare.ViewModels;

public class MoodLogViewModel
{
    [Required(ErrorMessage = "Please select a mood.")]
    [StringLength(20)]
    public string Mood { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Note { get; set; }
}
