namespace MindCare.Models;

public record AssessmentOption(string Text, int Score);

public record AssessmentQuestion(int Id, string Text, IReadOnlyList<AssessmentOption> Options);

public record AssessmentOutcome(string RiskLevel, string Recommendation);

public static class AssessmentQuestionnaire
{
    public const int MaximumOptionScore = 3;

    public static readonly IReadOnlyList<AssessmentQuestion> Questions =
    [
        CreateQuestion(1, "Over the last two weeks, how often have you felt nervous, anxious, or on edge?"),
        CreateQuestion(2, "Over the last two weeks, how often have you been unable to stop or control worrying?"),
        CreateQuestion(3, "Over the last two weeks, how often have you had little interest or pleasure in doing things?"),
        CreateQuestion(4, "Over the last two weeks, how often have you felt down, depressed, or hopeless?"),
        CreateQuestion(5, "Over the last two weeks, how often have you had trouble falling asleep, staying asleep, or sleeping too much?"),
        CreateQuestion(6, "Over the last two weeks, how often have you felt tired or had little energy?"),
        CreateQuestion(7, "Over the last two weeks, how often have you had trouble concentrating on everyday tasks?"),
        CreateQuestion(8, "Over the last two weeks, how often have you felt overwhelmed by your daily responsibilities?"),
        CreateQuestion(9, "Over the last two weeks, how often have you felt isolated or unsupported?"),
        CreateQuestion(10, "Over the last two weeks, how often have you found it difficult to relax or manage stress?")
    ];

    public static int MaximumScore => Questions.Count * MaximumOptionScore;

    public static AssessmentQuestion? FindQuestion(int questionId) =>
        Questions.SingleOrDefault(question => question.Id == questionId);

    public static AssessmentOutcome GetOutcome(int score)
    {
        if (score <= 9)
        {
            return new AssessmentOutcome(
                "Low",
                "Your responses indicate relatively low levels of distress. Continue maintaining healthy habits and monitoring your wellbeing.");
        }

        if (score <= 19)
        {
            return new AssessmentOutcome(
                "Moderate",
                "Your responses indicate some signs of distress. Consider talking with a mental health professional if these feelings continue or interfere with your daily life.");
        }

        return new AssessmentOutcome(
            "High",
            "Your responses indicate significant distress. Consider reaching out to a qualified mental health professional for support.");
    }

    private static AssessmentQuestion CreateQuestion(int id, string text) =>
        new(id, text,
        [
            new AssessmentOption("Not at all", 0),
            new AssessmentOption("Several days", 1),
            new AssessmentOption("More than half the days", 2),
            new AssessmentOption("Nearly every day", 3)
        ]);
}
