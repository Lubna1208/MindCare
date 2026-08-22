using System.ComponentModel.DataAnnotations;

namespace MindCare.Models;

public class AssessmentAnswer
{
    public int Id { get; set; }

    public int AssessmentId { get; set; }

    public Assessment Assessment { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int QuestionId { get; set; }

    [Required]
    [StringLength(100)]
    public string SelectedOption { get; set; } = string.Empty;

    [Range(0, 3)]
    public int Score { get; set; }
}
