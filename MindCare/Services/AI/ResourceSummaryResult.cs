namespace MindCare.Services.AI;

public sealed record ResourceSummaryResult(string Summary, IReadOnlyList<string> KeyPoints);
