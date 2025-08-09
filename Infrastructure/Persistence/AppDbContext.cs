using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPreference> Preferences => Set<UserPreference>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Forecast> Forecasts => Set<Forecast>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Preference)
             .WithOne(p => p.User)
             .HasForeignKey<UserPreference>(p => p.UserId);
        });

        b.Entity<UserPreference>(e => { e.HasKey(x => x.UserId); });

        b.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.FamilyId });
        });

        b.Entity<Forecast>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Lat, x.Lon, x.Timestamp });
        });
    }
}
