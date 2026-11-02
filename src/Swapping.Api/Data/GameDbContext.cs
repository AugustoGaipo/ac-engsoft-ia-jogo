using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Swapping.Api.Profiles;

namespace Swapping.Api.Data;

/// <summary>One database for the whole API (profiles + progress tables).</summary>
public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<ProfileEntity> Profiles => Set<ProfileEntity>();

    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();

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
    }
}
