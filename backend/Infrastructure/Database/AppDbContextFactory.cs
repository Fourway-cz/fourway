using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fourway.Api.Infrastructure.Database;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ConnectionStringVariable = "ConnectionStrings__DefaultConnection";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Pro EF Core nástroje nastav proměnnou prostředí {ConnectionStringVariable}. " +
                "Například: ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;" +
                "Database=fourway_db;Username=postgres;Password=SuperSecurePassword'");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        DatabaseExtensions.ConfigureNpgsql(optionsBuilder, connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
