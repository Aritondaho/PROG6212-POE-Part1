using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// EF Core context for the RaceDay schema (Code-First).
/// The Fluent API below mirrors the CREATE TABLE statements in docs/Database SQL Script.sql
/// so the migration produces the same tables, keys, constraints and defaults as the Part 1 plan.
/// </summary>
public class RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Organiser> Organisers => Set<Organiser>();

    public DbSet<Participant> Participants => Set<Participant>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Enrolment> Enrolments => Set<Enrolment>();

    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", t =>
                t.HasCheckConstraint("CK_Users_Role", "Role IN ('Organiser', 'Participant')"));

            entity.HasKey(u => u.UserID);
            entity.Property(u => u.Email).HasMaxLength(255).IsRequired();
            // A unique index rather than an alternate key: EF does not allow key columns to change, and users can edit their email.
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(20).IsRequired();
            entity.Property(u => u.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<Organiser>(entity =>
        {
            entity.ToTable("Organisers");

            entity.HasKey(o => o.OrganiserID);
            entity.Property(o => o.OrganisationName).HasMaxLength(100).IsRequired();
            entity.Property(o => o.ContactNumber).HasMaxLength(20).IsRequired();

            entity.HasOne(o => o.User)
                .WithOne(u => u.Organiser)
                .HasForeignKey<Organiser>(o => o.UserID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Participant>(entity =>
        {
            entity.ToTable("Participants");

            entity.HasKey(p => p.ParticipantID);
            entity.Property(p => p.DateOfBirth).HasColumnType("date").IsRequired();
            entity.Property(p => p.ProfilePictureURL).HasMaxLength(500);

            entity.HasOne(p => p.User)
                .WithOne(u => u.Participant)
                .HasForeignKey<Participant>(p => p.UserID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events", t =>
                t.HasCheckConstraint("CK_Events_EventType", "EventType IN ('Run', 'Walk', 'Cycle')"));

            entity.HasKey(e => e.EventID);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            entity.Property(e => e.EventDate).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.Location).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Distance).HasColumnType("decimal(5,2)").IsRequired();
            entity.Property(e => e.EventType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.BannerImageURL).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");

            // Deleting an organiser must never silently remove their events.
            entity.HasOne(e => e.Organiser)
                .WithMany(o => o.Events)
                .HasForeignKey(e => e.OrganiserID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");

            entity.HasKey(c => c.CategoryID);
            entity.Property(c => c.Name).HasMaxLength(50).IsRequired();
            entity.Property(c => c.Description).HasMaxLength(200);

            // Categories are owned by their event, so they are removed with it.
            entity.HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolments", t =>
                t.HasCheckConstraint("CK_Enrolments_Status", "Status IN ('Pending', 'Confirmed', 'Cancelled')"));

            entity.HasKey(en => en.EnrolmentID);
            entity.Property(en => en.RegistrationDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(en => en.Status).HasMaxLength(20).HasDefaultValue(EnrolmentStatuses.Pending).IsRequired();
            entity.HasAlternateKey(en => new { en.ParticipantID, en.EventID }).HasName("UQ_Enrolment");

            entity.HasOne(en => en.Participant)
                .WithMany(p => p.Enrolments)
                .HasForeignKey(en => en.ParticipantID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(en => en.Event)
                .WithMany(e => e.Enrolments)
                .HasForeignKey(en => en.EventID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(en => en.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(en => en.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Results", t =>
                t.HasCheckConstraint("CK_Results_Status", "Status IN ('Published', 'Pending', 'Disqualified')"));

            entity.HasKey(r => r.ResultID);
            entity.Property(r => r.FinishTime).HasColumnType("time").IsRequired();
            entity.Property(r => r.FinishPosition).IsRequired();
            entity.Property(r => r.Status).HasMaxLength(20).HasDefaultValue(ResultStatuses.Published).IsRequired();

            // One result per enrolment: the Part 1 schema marks EnrolmentID as UNIQUE.
            entity.HasIndex(r => r.EnrolmentID).IsUnique();
            entity.HasOne(r => r.Enrolment)
                .WithOne(en => en.Result)
                .HasForeignKey<Result>(r => r.EnrolmentID)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
