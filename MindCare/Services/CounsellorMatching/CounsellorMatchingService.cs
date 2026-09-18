using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;
using MindCare.ViewModels;

namespace MindCare.Services.CounsellorMatching;

public sealed class CounsellorMatchingService(
    ApplicationDbContext context,
    ILogger<CounsellorMatchingService> logger) : ICounsellorMatchingService
{
    // These are the approved specialization labels used by counsellor profiles.  Keep
    // matching constrained to this mapping rather than trying to infer relevance from
    // arbitrary profile text.
    private static readonly IReadOnlyDictionary<CounsellorConcern, IReadOnlySet<string>> ConcernSpecializations =
        new Dictionary<CounsellorConcern, IReadOnlySet<string>>
        {
            [CounsellorConcern.Stress] = Specializations("Stress Management", "Anxiety & Stress", "General Counselling"),
            [CounsellorConcern.Anxiety] = Specializations("Anxiety & Stress", "General Counselling"),
            [CounsellorConcern.StudyPressure] = Specializations("Student Wellbeing", "Student & Academic Counselling", "General Counselling"),
            [CounsellorConcern.RelationshipConcerns] = Specializations("Relationship Counselling", "Family Counselling"),
            [CounsellorConcern.GeneralWellbeing] = Specializations("Student Wellbeing", "General Counselling")
        };

    public async Task<IReadOnlyList<CounsellorMatchResultViewModel>> FindMatchesAsync(
        CounsellorConcern concern,
        CancellationToken cancellationToken = default)
    {
        if (!ConcernSpecializations.TryGetValue(concern, out var mappedSpecializations))
        {
            return [];
        }

        var now = DateTime.Now;
        var profiles = await (
            from profile in context.CounsellorProfiles.AsNoTracking()
            join userRole in context.UserRoles on profile.ApplicationUserId equals userRole.UserId
            join role in context.Roles on userRole.RoleId equals role.Id
            where role.Name == RoleNames.Counsellor
            select profile)
            .Include(profile => profile.ApplicationUser)
            .Include(profile => profile.AvailabilitySlots)
            .ThenInclude(slot => slot.Appointment)
            .ToListAsync(cancellationToken);

        var activeProfiles = profiles
            .Where(profile => IsAccountAvailable(profile.ApplicationUser, now))
            .ToList();

        var specializationCandidates = activeProfiles
            .Select(profile => new
            {
                Profile = profile,
                Score = GetSpecializationScore(profile.Specialization, mappedSpecializations)
            })
            .Where(candidate => candidate.Score > 0)
            .ToList();

        var availabilityCandidates = specializationCandidates
            .Select(candidate => new
            {
                candidate.Profile,
                candidate.Score,
                NextSlot = candidate.Profile.AvailabilitySlots
                    .Where(slot => IsSlotBookable(slot, now))
                    .OrderBy(slot => slot.Date)
                    .ThenBy(slot => slot.StartTime)
                    .FirstOrDefault()
            })
            .Where(candidate => candidate.NextSlot is not null)
            .ToList();

        logger.LogInformation(
            "Counsellor matching: concern {Concern}; active counsellors {ActiveCounsellorCount}; candidates after specialization {SpecializationCandidateCount}; candidates after future availability {AvailabilityCandidateCount}.",
            GetConcernDisplayName(concern),
            activeProfiles.Count,
            specializationCandidates.Count,
            availabilityCandidates.Count);

        return availabilityCandidates
            .OrderByDescending(candidate => candidate.Score + 1) // +1 for verified future availability.
            .ThenBy(candidate => candidate.NextSlot!.Date)
            .ThenBy(candidate => candidate.NextSlot!.StartTime)
            .ThenBy(candidate => candidate.Profile.ApplicationUser.Name)
            .Take(3)
            .Select(candidate => new CounsellorMatchResultViewModel
            {
                CounsellorProfileId = candidate.Profile.Id,
                CounsellorName = candidate.Profile.ApplicationUser.Name,
                Specialization = candidate.Profile.Specialization,
                NextAvailableDate = candidate.NextSlot!.Date,
                NextAvailableStartTime = candidate.NextSlot.StartTime,
                Explanation = CreateFallbackExplanation(concern, candidate.Profile.Specialization)
            })
            .ToList();
    }

    public static string GetConcernDisplayName(CounsellorConcern concern) => concern switch
    {
        CounsellorConcern.StudyPressure => "Study Pressure",
        CounsellorConcern.RelationshipConcerns => "Relationship Concerns",
        CounsellorConcern.GeneralWellbeing => "General Wellbeing",
        _ => concern.ToString()
    };

    private static bool IsAccountAvailable(ApplicationUser user, DateTime now) =>
        !user.LockoutEnabled || !user.LockoutEnd.HasValue || user.LockoutEnd.Value.LocalDateTime <= now;

    // This mirrors the booking invariant: a slot must be in the future, marked open,
    // and have no appointment already associated with it.
    private static bool IsSlotBookable(AvailabilitySlot slot, DateTime now) =>
        !slot.IsBooked &&
        slot.Appointment is null &&
        (slot.Date > now.Date || (slot.Date == now.Date && slot.StartTime > now.TimeOfDay));

    private static int GetSpecializationScore(string specialization, IReadOnlySet<string> mappedSpecializations)
    {
        return mappedSpecializations.Contains(Normalize(specialization)) ? 3 : 0;
    }

    private static string CreateFallbackExplanation(CounsellorConcern concern, string specialization) =>
        $"This counsellor lists {specialization} among their areas of support, which may relate to the {GetConcernDisplayName(concern)} concern you selected.";

    private static string Normalize(string value) => new(value
        .Where(char.IsLetterOrDigit)
        .Select(char.ToLowerInvariant)
        .ToArray());

    private static IReadOnlySet<string> Specializations(params string[] values) =>
        new HashSet<string>(values.Select(Normalize), StringComparer.Ordinal);
}
