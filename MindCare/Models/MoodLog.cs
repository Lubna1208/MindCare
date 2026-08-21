using System.ComponentModel.DataAnnotations;

namespace MindCare.Models;

public class MoodLog
{
    public static readonly IReadOnlyList<string> AvailableMoods =
    [
        "Great",
        "Good",
        "Okay",
        "Sad",
        "Very Sad",
        "Angry",
        "Anxious"
    ];

    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    [Required(ErrorMessage = "Please select a mood.")]
    [StringLength(20)]
    public string Mood { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public static bool IsValidMood(string? mood) => AvailableMoods.Contains(mood);
}
