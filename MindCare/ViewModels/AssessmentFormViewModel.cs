using System.ComponentModel.DataAnnotations;
using MindCare.Models;

namespace MindCare.ViewModels;

public class AssessmentFormViewModel
{
    public List<AssessmentQuestionFormViewModel> Questions { get; set; } = [];
}

public class AssessmentQuestionFormViewModel
{
    public int QuestionId { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    public IReadOnlyList<AssessmentOption> Options { get; set; } = [];

    [Required(ErrorMessage = "Please select an answer for this question.")]
    public int? SelectedScore { get; set; }
}
