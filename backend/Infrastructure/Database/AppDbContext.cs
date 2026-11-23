using System.Reflection;
using Fourway.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql.NameTranslation;
using Route = Fourway.Api.Models.Route;

namespace Fourway.Api.Infrastructure.Database;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private static readonly NpgsqlSnakeCaseNameTranslator NameTranslator = new();

    public DbSet<Example> Examples => Set<Example>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<Agency> Agencies => Set<Agency>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<IdsSystem> IdsSystems => Set<IdsSystem>();
    public DbSet<RouteIdsInfo> RouteIdsInfos => Set<RouteIdsInfo>();
    public DbSet<Stop> Stops => Set<Stop>();
    public DbSet<StopSourceRef> StopSourceRefs => Set<StopSourceRef>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServicePattern> ServicePatterns => Set<ServicePattern>();
    public DbSet<ServiceException> ServiceExceptions => Set<ServiceException>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripOperator> TripOperators => Set<TripOperator>();
    public DbSet<StopTime> StopTimes => Set<StopTime>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.HasPostgresExtension("unaccent");

        modelBuilder.HasPostgresEnum<ExampleStatus>(
            name: "example_status",
            nameTranslator: NameTranslator);
        modelBuilder.HasPostgresEnum<AgencyLegalForm>(
            name: "agency_legal_form",
            nameTranslator: NameTranslator);
        modelBuilder.HasPostgresEnum<RouteLineType>(
            name: "route_line_type",
            nameTranslator: NameTranslator);
        modelBuilder.HasPostgresEnum<VehicleMode>(
            name: "vehicle_mode",
            nameTranslator: NameTranslator);
        modelBuilder.HasPostgresEnum<StopKind>(
            name: "stop_kind",
            nameTranslator: NameTranslator);
        modelBuilder.HasPostgresEnum<ServiceExceptionType>(
            name: "service_exception_type",
            nameTranslator: NameTranslator);
        modelBuilder.HasPostgresEnum<WeekParity>(
            name: "week_parity",
            nameTranslator: NameTranslator);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
