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

    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<ResourceCategory> ResourceCategories => Set<ResourceCategory>();
    public DbSet<ResourceBookmark> ResourceBookmarks => Set<ResourceBookmark>();
    public DbSet<ResourceReviewLog> ResourceReviewLogs => Set<ResourceReviewLog>();

    public DbSet<AvailabilitySlot> AvailabilitySlots => Set<AvailabilitySlot>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<TrustedContact> TrustedContacts => Set<TrustedContact>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    public DbSet<ForumPost> ForumPosts => Set<ForumPost>();

    public DbSet<ForumComment> ForumComments => Set<ForumComment>();

    public DbSet<ForumReport> ForumReports => Set<ForumReport>();

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

        builder.Entity<Resource>()
            .Property(resource => resource.Content)
            .HasColumnType("nvarchar(max)");

        builder.Entity<Resource>().HasOne(r => r.ResourceCategory).WithMany(c => c.Resources)
            .HasForeignKey(r => r.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Resource>().HasOne(r => r.CreatedByUser).WithMany()
            .HasForeignKey(r => r.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Resource>().HasOne(r => r.ReviewedByAdmin).WithMany()
            .HasForeignKey(r => r.ReviewedByAdminId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Resource>().HasIndex(r => new { r.Status, r.CategoryId });
        builder.Entity<ResourceCategory>().HasIndex(c => c.Name).IsUnique();
        builder.Entity<ResourceBookmark>().HasIndex(b => new { b.UserId, b.ResourceId }).IsUnique();
        builder.Entity<ResourceBookmark>().HasOne(b => b.User).WithMany().HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ResourceBookmark>().HasOne(b => b.Resource).WithMany(r => r.Bookmarks).HasForeignKey(b => b.ResourceId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ResourceReviewLog>().HasOne(l => l.Resource).WithMany().HasForeignKey(l => l.ResourceId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AvailabilitySlot>()
            .HasOne(slot => slot.CounsellorProfile)
            .WithMany(profile => profile.AvailabilitySlots)
            .HasForeignKey(slot => slot.CounsellorProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AvailabilitySlot>()
            .HasIndex(slot => new { slot.CounsellorProfileId, slot.Date, slot.StartTime, slot.EndTime })
            .IsUnique();

        builder.Entity<Appointment>()
            .HasOne(appointment => appointment.User)
            .WithMany(user => user.Appointments)
            .HasForeignKey(appointment => appointment.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appointment>()
            .HasOne(appointment => appointment.CounsellorProfile)
            .WithMany(profile => profile.Appointments)
            .HasForeignKey(appointment => appointment.CounsellorProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Appointment>()
            .HasOne(appointment => appointment.AvailabilitySlot)
            .WithOne(slot => slot.Appointment)
            .HasForeignKey<Appointment>(appointment => appointment.AvailabilitySlotId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Appointment>()
            .HasIndex(appointment => appointment.AvailabilitySlotId)
            .IsUnique();

        builder.Entity<Message>()
            .HasOne(message => message.Appointment)
            .WithMany(appointment => appointment.Messages)
            .HasForeignKey(message => message.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Message>()
            .HasOne(message => message.SenderUser)
            .WithMany(user => user.SentMessages)
            .HasForeignKey(message => message.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Message>()
            .HasOne(message => message.ReceiverUser)
            .WithMany(user => user.ReceivedMessages)
            .HasForeignKey(message => message.ReceiverUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Message>()
            .HasIndex(message => new { message.AppointmentId, message.SentAt });

        builder.Entity<TrustedContact>()
            .HasOne(contact => contact.User)
            .WithMany(user => user.TrustedContacts)
            .HasForeignKey(contact => contact.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TrustedContact>()
            .HasIndex(contact => new { contact.UserId, contact.Name });

        builder.Entity<Notification>()
            .HasOne(notification => notification.User)
            .WithMany(user => user.Notifications)
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Notification>()
            .HasIndex(notification => new { notification.UserId, notification.EventKey })
            .IsUnique()
            .HasFilter("[EventKey] IS NOT NULL");

        builder.Entity<NotificationPreference>()
            .HasOne(preference => preference.User)
            .WithOne(user => user.NotificationPreference)
            .HasForeignKey<NotificationPreference>(preference => preference.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<NotificationPreference>()
            .HasIndex(preference => preference.UserId)
            .IsUnique();

        builder.Entity<ForumPost>()
            .HasOne(post => post.User)
            .WithMany(user => user.ForumPosts)
            .HasForeignKey(post => post.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ForumPost>()
            .HasIndex(post => post.CreatedAt);

        builder.Entity<ForumComment>()
            .HasOne(comment => comment.ForumPost)
            .WithMany(post => post.Comments)
            .HasForeignKey(comment => comment.ForumPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ForumComment>()
            .HasOne(comment => comment.User)
            .WithMany(user => user.ForumComments)
            .HasForeignKey(comment => comment.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ForumComment>()
            .HasIndex(comment => new { comment.ForumPostId, comment.CreatedAt });

        builder.Entity<ForumReport>()
            .HasOne(report => report.Post)
            .WithMany(post => post.Reports)
            .HasForeignKey(report => report.PostId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ForumReport>()
            .HasOne(report => report.Comment)
            .WithMany(comment => comment.Reports)
            .HasForeignKey(report => report.CommentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ForumReport>()
            .HasIndex(report => new { report.ReporterUserId, report.PostId, report.Status })
            .IsUnique()
            .HasFilter("[PostId] IS NOT NULL AND [Status] = 'Pending'");

        builder.Entity<ForumReport>()
            .HasIndex(report => new { report.ReporterUserId, report.CommentId, report.Status })
            .IsUnique()
            .HasFilter("[CommentId] IS NOT NULL AND [Status] = 'Pending'");
    }
}
