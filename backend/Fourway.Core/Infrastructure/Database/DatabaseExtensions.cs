using Fourway.Core.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql.NameTranslation;

namespace Fourway.Core.Infrastructure.Database;

public static class DatabaseExtensions
{
    private const string ConnectionStringName = "DefaultConnection";
    private static readonly NpgsqlSnakeCaseNameTranslator NameTranslator = new();

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
                npgsql.UseNetTopologySuite();
                npgsql.MapEnum<ExampleStatus>(
                    "example_status",
                    nameTranslator: NameTranslator);
                npgsql.MapEnum<AgencyLegalForm>(
                    "agency_legal_form",
                    nameTranslator: NameTranslator);
                npgsql.MapEnum<RouteLineType>(
                    "route_line_type",
                    nameTranslator: NameTranslator);
                npgsql.MapEnum<VehicleMode>(
                    "vehicle_mode",
                    nameTranslator: NameTranslator);
                npgsql.MapEnum<StopKind>(
                    "stop_kind",
                    nameTranslator: NameTranslator);
                npgsql.MapEnum<ServiceExceptionType>(
                    "service_exception_type",
                    nameTranslator: NameTranslator);
                npgsql.MapEnum<WeekParity>(
                    "week_parity",
                    nameTranslator: NameTranslator);
            })
            .UseSnakeCaseNamingConvention();
    }
}
