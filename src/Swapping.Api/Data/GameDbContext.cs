using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Swapping.Api.Profiles;
using Swapping.Api.Progress;

namespace Swapping.Api.Data;

/// <summary>One database for the whole API (profiles + progress tables).</summary>
public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<ProfileEntity> Profiles => Set<ProfileEntity>();

    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();

    public DbSet<BestResultEntity> BestResults => Set<BestResultEntity>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot compare or sort DateTimeOffset; every time is stored in UTC as a number.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProfileEntity>(profile =>
        {
            profile.ToTable("Profiles");
            profile.HasKey(p => p.Id);
            profile.HasIndex(p => p.NameKey).IsUnique();
            profile.Property(p => p.Name).HasMaxLength(ProfileNames.MaxLength);
            profile.Property(p => p.NameKey).HasMaxLength(ProfileNames.MaxLength);
        });

        modelBuilder.Entity<SessionEntity>(session =>
        {
            session.ToTable("Sessions");
            session.HasKey(s => s.Id);
            session.HasIndex(s => s.TokenHash).IsUnique();
            session.HasIndex(s => s.ProfileId);
            session.HasOne<ProfileEntity>().WithMany().HasForeignKey(s => s.ProfileId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BestResultEntity>(best =>
        {
            best.ToTable("BestResults");
            best.HasKey(b => new { b.ProfileId, b.LevelId, b.Mode });
            best.Ignore(b => b.Score);
            best.Property(b => b.LevelId).HasMaxLength(64);
            best.Property(b => b.Mode).HasConversion<string>().HasMaxLength(8);
            best.HasOne<ProfileEntity>().WithMany().HasForeignKey(b => b.ProfileId).OnDelete(DeleteBehavior.Cascade);
            // The two ranking orders (P7): a top-N or a COUNT for "my rank" walks one index.
            best.HasIndex(b => new { b.LevelId, b.Mode, b.TimeMs, b.Swaps, b.AchievedAt }).HasDatabaseName("IX_BestResults_TimeOrder");
            best.HasIndex(b => new { b.LevelId, b.Mode, b.Swaps, b.TimeMs, b.AchievedAt }).HasDatabaseName("IX_BestResults_MovesOrder");
        });
    }
}
