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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<MoodLog>()
            .HasOne(moodLog => moodLog.User)
            .WithMany(user => user.MoodLogs)
            .HasForeignKey(moodLog => moodLog.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
