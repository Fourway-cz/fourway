using System.Text.Json;
using System.Text.Json.Serialization;
using FastEndpoints;
using Fourway.Api.Features.Common;

namespace Fourway.Api.Infrastructure.Endpoints;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpointServices(this IServiceCollection services)
    {
        services.AddFastEndpoints();
        return services;
    }

    public static IApplicationBuilder UseEndpointPipeline(this IApplicationBuilder app)
    {
        return app.UseFastEndpoints(options =>
        {
            options.Versioning.Prefix = "v";
            options.Versioning.PrependToRoute = true;
            options.Endpoints.ShortNames = true;
            options.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.Serializer.Options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            options.Serializer.Options.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;

            options.Errors.ContentType = "application/json";
            options.Errors.ResponseBuilder = (failures, _, _) =>
            {
                var firstError = failures.FirstOrDefault()?.ErrorMessage ?? "Neplatný požadavek.";
                return new ErrorResponse(firstError);
            };

            options.Errors.ProducesMetadataType = typeof(ErrorResponse);
        });
    }
}
