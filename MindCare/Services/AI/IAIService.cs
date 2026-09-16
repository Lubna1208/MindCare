namespace MindCare.Services.AI;

public interface IAIService
{
    Task<string> AskFaqAsync(string question, CancellationToken cancellationToken = default);

    Task<ResourceSummaryResult> SummarizeResourceAsync(
        string title,
        string content,
        CancellationToken cancellationToken = default);
}
