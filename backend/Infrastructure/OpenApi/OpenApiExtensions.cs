using FastEndpoints.Swagger;
using NJsonSchema;
using NSwag.AspNetCore;

namespace Fourway.Api.Infrastructure.OpenApi;

public static class OpenApiExtensions
{
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        services.SwaggerDocument(options =>
        {
            options.MaxEndpointVersion = 1;
            options.ShortSchemaNames = true;

            options.DocumentSettings = settings =>
            {
                settings.Title = "Fourway API";
                settings.Version = "v1";
                settings.Description = "Backendové API aplikace Fourway.";
                settings.MarkNonNullablePropsAsRequired();

                settings.PostProcess = document =>
                {
                    document.SchemaType = SchemaType.Swagger2;
                };
            };
        });

        return services;
    }

    public static IApplicationBuilder UseOpenApiDocumentation(this IApplicationBuilder app)
    {
        return app.UseSwaggerGen(
            config: null,
            uiConfig: opt =>
            {
                opt.SwaggerRoutes.Clear();
                opt.SwaggerRoutes.Add(new SwaggerUiRoute("v1", "./swagger/v1/swagger.json"));
            }
        );
    }
}
