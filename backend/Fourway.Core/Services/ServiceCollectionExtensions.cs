namespace Fourway.Core.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJourneyServices(this IServiceCollection services)
    {
        services.AddScoped<IJourneySearchService, SimpleJourneySearchService>();
        services.AddScoped<IDepartureService, SimpleDepartureService>();
        return services;
    }
}
