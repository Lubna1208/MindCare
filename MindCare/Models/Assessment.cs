using System.ComponentModel.DataAnnotations;

namespace MindCare.Models;

public class Assessment
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int Score { get; set; }

    [Required]
    [StringLength(20)]
    public string RiskLevel { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ICollection<AssessmentAnswer> Answers { get; set; } = new List<AssessmentAnswer>();
}
