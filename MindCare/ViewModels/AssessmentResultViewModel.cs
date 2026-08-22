using MindCare.Models;

namespace MindCare.ViewModels;

public class AssessmentResultViewModel
{
    public int Id { get; set; }

    public int Score { get; set; }

    public string RiskLevel { get; set; } = string.Empty;

    public string Recommendation { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsHighRisk => RiskLevel == "High";
}
