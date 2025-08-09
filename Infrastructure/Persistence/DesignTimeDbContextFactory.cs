using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Prefer env var if present; fallback to local default
        var cs = Environment.GetEnvironmentVariable("DB_CONNECTION")
                 ?? "Host=localhost;Port=5432;Database=weather;Username=app;Password=app";

        optionsBuilder.UseNpgsql(cs);

        return new AppDbContext(optionsBuilder.Options);
    }
}
