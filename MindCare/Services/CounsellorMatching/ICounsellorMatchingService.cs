using MindCare.ViewModels;

namespace MindCare.Services.CounsellorMatching;

public interface ICounsellorMatchingService
{
    Task<IReadOnlyList<CounsellorMatchResultViewModel>> FindMatchesAsync(
        CounsellorConcern concern,
        CancellationToken cancellationToken = default);
}
