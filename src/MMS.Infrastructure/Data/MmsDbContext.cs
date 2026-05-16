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
    public DbSet<MMS.Domain.Entities.Task> Tasks { get; set; } = null!;
    public DbSet<Region> Regions { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;
    public DbSet<UserInvite> UserInvites { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<BoardVote> BoardVotes { get; set; } = null!;
    public DbSet<Annotation> Annotations { get; set; } = null!;
    public DbSet<Ranking> Rankings { get; set; } = null!;
    public DbSet<ReaderVote> ReaderVotes { get; set; } = null!;

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
            .WithOne(s => s.Author)
            .HasForeignKey(s => s.AuthorId)
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

        // Submission -> Vote
        modelBuilder.Entity<Submission>()
            .HasMany(s => s.Votes)
            .WithOne(v => v.Submission)
            .HasForeignKey(v => v.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Vote -> BoardMember
        modelBuilder.Entity<BoardVote>()
            .HasOne(v => v.BoardMember)
            .WithMany(u => u.BoardVotes)
            .HasForeignKey(v => v.BoardMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Task relations
        modelBuilder.Entity<MMS.Domain.Entities.Task>()
            .HasOne(t => t.AssignedUser)
            .WithMany(u => u.Tasks)
            .HasForeignKey(t => t.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MMS.Domain.Entities.Task>()
            .HasOne(t => t.Region)
            .WithMany(r => r.Tasks)
            .HasForeignKey(t => t.RegionId)
            .OnDelete(DeleteBehavior.SetNull);

        // ReaderVote -> Series
        modelBuilder.Entity<ReaderVote>()
            .HasOne(v => v.Series)
            .WithMany(s => s.ReaderVotes)
            .HasForeignKey(v => v.SeriesId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- ENUM TO STRING CONVERSIONS ---
        modelBuilder.Entity<MMS.Domain.Entities.Task>()
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
    }
}
