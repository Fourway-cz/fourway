using Fourway.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql.NameTranslation;

namespace Fourway.Api.Infrastructure.Database;

public static class DatabaseExtensions
{
    private const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Chybí connection string '{ConnectionStringName}'. " +
                "V dev prostředí ho nastavuje compose.dev.yaml, v produkci ho předávej přes " +
                "proměnnou prostředí ConnectionStrings__DefaultConnection.");
        }

        services.AddDbContext<AppDbContext>(
            options => ConfigureNpgsql(options, connectionString));

        return services;
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await DatabaseInitializer.InitializeAsync(
            app.Services,
            app.Logger,
            app.Lifetime.ApplicationStopping);
    }

    internal static void ConfigureNpgsql(
        DbContextOptionsBuilder options,
        string connectionString)
    {
        options
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null);
                npgsql.MapEnum<ExampleStatus>(
                    "example_status",
                    nameTranslator: new NpgsqlSnakeCaseNameTranslator());
            })
            .UseSnakeCaseNamingConvention();
    }
}
