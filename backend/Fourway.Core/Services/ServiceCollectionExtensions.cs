namespace Fourway.Core.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJourneyServices(this IServiceCollection services)
    {
        services.AddScoped<IJourneySearchService, SimpleJourneySearchService>();
        return services;
    }
}
