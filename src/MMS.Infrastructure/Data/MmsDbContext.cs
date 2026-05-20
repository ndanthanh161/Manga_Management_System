using Microsoft.EntityFrameworkCore;
using MMS.Domain.Entities;
using MMS.Domain.Enums;

namespace MMS.Infrastructure.Data;

public class MmsDbContext : DbContext
{
    public MmsDbContext(DbContextOptions<MmsDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Series> Series { get; set; } = null!;
    public DbSet<Chapter> Chapters { get; set; } = null!;
    public DbSet<Page> Pages { get; set; } = null!;
    public DbSet<Manuscript> Manuscripts { get; set; } = null!;
    public DbSet<Asset> Assets { get; set; } = null!;
    public DbSet<Submission> Submissions { get; set; } = null!;
    public DbSet<WorkTask> Tasks { get; set; } = null!;
    public DbSet<Region> Regions { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;
    public DbSet<UserInvite> UserInvites { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<BoardVote> BoardVotes { get; set; } = null!;
    public DbSet<Annotation> Annotations { get; set; } = null!;
    public DbSet<Ranking> Rankings { get; set; } = null!;
    public DbSet<ReaderVote> ReaderVotes { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<AssistantSalary> AssistantSalaries { get; set; } = null!;
    public DbSet<AssistantIncome> AssistantIncomes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Many-to-many User <-> Role
        modelBuilder.Entity<User>()
            .HasMany(u => u.Roles)
            .WithMany(r => r.Users)
            .UsingEntity(j => j.ToTable("UserRoles"));

        // User relationships
        modelBuilder.Entity<User>()
            .HasMany(u => u.Series)
            .WithOne(s => s.Author!)
            .HasForeignKey(s => s.AuthorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Series>()
            .HasOne(s => s.TantouEditor)
            .WithMany()
            .HasForeignKey(s => s.TantouEditorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Mangaka <-> Assistant self-referencing relationship
        modelBuilder.Entity<User>()
            .HasOne(u => u.Mangaka)
            .WithMany(u => u.Assistants)
            .HasForeignKey(u => u.MangakaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Series -> Chapter
        modelBuilder.Entity<Series>()
            .HasMany(s => s.Chapters)
            .WithOne(c => c.Series)
            .HasForeignKey(c => c.SeriesId)
            .OnDelete(DeleteBehavior.Cascade);

        // Chapter -> Page
        modelBuilder.Entity<Chapter>()
            .HasMany(c => c.Pages)
            .WithOne(p => p.Chapter)
            .HasForeignKey(p => p.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Manuscript -> Asset
        modelBuilder.Entity<Manuscript>()
            .HasMany(m => m.Assets)
            .WithOne(a => a.Manuscript)
            .HasForeignKey(a => a.ManuscriptId)
            .OnDelete(DeleteBehavior.Cascade);

        // Submission -> Series
        modelBuilder.Entity<Submission>()
            .HasOne(s => s.Series)
            .WithMany()
            .HasForeignKey(s => s.SeriesId)
            .OnDelete(DeleteBehavior.Restrict);

        // Submission -> Mangaka (User)
        modelBuilder.Entity<Submission>()
            .HasOne(s => s.Mangaka)
            .WithMany()
            .HasForeignKey(s => s.MangakaId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Submission -> Votes
        modelBuilder.Entity<Submission>()
            .HasMany(s => s.Votes)
            .WithOne(v => v.Submission)
            .HasForeignKey(v => v.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Submission -> Manuscripts
        modelBuilder.Entity<Submission>()
            .HasMany(s => s.Manuscripts)
            .WithOne(m => m.Submission)
            .HasForeignKey(m => m.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Region -> Page
        modelBuilder.Entity<Region>()
            .HasOne(r => r.Page)
            .WithMany()
            .HasForeignKey(r => r.PageId)
            .OnDelete(DeleteBehavior.SetNull);
            
        // Vote -> BoardMember
        modelBuilder.Entity<BoardVote>()
            .HasOne(v => v.BoardMember)
            .WithMany(u => u.BoardVotes)
            .HasForeignKey(v => v.BoardMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Task relations
        modelBuilder.Entity<WorkTask>()
            .HasOne(t => t.AssignedUser)
            .WithMany(u => u.Tasks)
            .HasForeignKey(t => t.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<WorkTask>()
            .HasOne(t => t.Region)
            .WithMany(r => r.Tasks)
            .HasForeignKey(t => t.RegionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<WorkTask>()
            .Property(t => t.Reward)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        // ReaderVote -> Series
        modelBuilder.Entity<ReaderVote>()
            .HasOne(v => v.Series)
            .WithMany(s => s.ReaderVotes)
            .HasForeignKey(v => v.SeriesId)
            .OnDelete(DeleteBehavior.Cascade);

        // AssistantSalary mappings
        modelBuilder.Entity<AssistantSalary>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Assistant)
                .WithOne(u => u.SalaryConfig)
                .HasForeignKey<AssistantSalary>(e => e.AssistantId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.BaseSalary)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);
            entity.HasIndex(e => e.AssistantId)
                .IsUnique();
        });

        // AssistantIncome mappings
        modelBuilder.Entity<AssistantIncome>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Assistant)
                .WithMany(u => u.Incomes)
                .HasForeignKey(e => e.AssistantId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.BaseSalary)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);
            entity.Property(e => e.PerformanceBonus)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);
            entity.Property(e => e.TotalIncome)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);
            entity.HasIndex(e => new { e.AssistantId, e.Year, e.Month })
                .IsUnique();
        });

        // --- ENUM TO STRING CONVERSIONS ---
        modelBuilder.Entity<WorkTask>()
            .Property(t => t.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Series>()
            .Property(s => s.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Manuscript>()
            .Property(m => m.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Submission>()
            .Property(s => s.Status)
            .HasConversion<string>();

        modelBuilder.Entity<BoardVote>()
            .Property(v => v.VoteType)
            .HasConversion<string>();

        // AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.EntityDisplayName).HasMaxLength(256);
            entity.Property(e => e.OldValueJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.NewValueJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.Source).HasMaxLength(64);
            entity.Property(e => e.Action).HasConversion<string>();

            // Indexes
            entity.HasIndex(e => e.ActorUserId);
            entity.HasIndex(e => new { e.EntityName, e.EntityId });
            entity.HasIndex(e => e.CorrelationId);

            // Index quan trọng cho việc chia partition hoặc filter theo thời gian sau này
            entity.HasIndex(e => e.OccurredAt).IsClustered(false);
        });

    }
}
