namespace MindCare.Services.AI;

public sealed record CounsellorMatchExplanationCandidate(
    int CounsellorProfileId,
    string CounsellorName,
    string Specialization,
    string AvailabilitySummary);
