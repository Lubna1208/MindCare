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

    public DbSet<AvailabilitySlot> AvailabilitySlots => Set<AvailabilitySlot>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<Message> Messages => Set<Message>();

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
    }
}
