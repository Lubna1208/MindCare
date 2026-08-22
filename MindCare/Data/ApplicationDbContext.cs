using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MindCare.Models;

namespace MindCare.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<CounsellorProfile> CounsellorProfiles => Set<CounsellorProfile>();

    public DbSet<MoodLog> MoodLogs => Set<MoodLog>();

    public DbSet<Assessment> Assessments => Set<Assessment>();

    public DbSet<AssessmentAnswer> AssessmentAnswers => Set<AssessmentAnswer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<MoodLog>()
            .HasOne(moodLog => moodLog.User)
            .WithMany(user => user.MoodLogs)
            .HasForeignKey(moodLog => moodLog.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Assessment>()
            .HasOne(assessment => assessment.User)
            .WithMany(user => user.Assessments)
            .HasForeignKey(assessment => assessment.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AssessmentAnswer>()
            .HasOne(answer => answer.Assessment)
            .WithMany(assessment => assessment.Answers)
            .HasForeignKey(answer => answer.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
