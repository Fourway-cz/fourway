using System.Text;
using FastEndpoints.Swagger;
using NJsonSchema;
using NSwag;
using NSwag.AspNetCore;

namespace Fourway.Core.Infrastructure.OpenApi;

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
                    HoistParameterTypes(document);
                    CamelCaseQueryParameters(document);
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

    // Generátor vytváří parametry ve stylu OpenAPI 3, kde typ visí ve `schema`,
    // ale dokument se vydává jako Swagger 2.0, který u non-body parametrů typ
    // očekává přímo na parametru. Bez toho Swagger UI typy nezobrazí.
    private static void HoistParameterTypes(NSwag.OpenApiDocument document)
    {
        var parameters = document.Paths.Values
            .SelectMany(path => path.Parameters.Concat(path.Values.SelectMany(o => o.Parameters)));

        foreach (var parameter in parameters)
        {
            if (parameter.Kind == OpenApiParameterKind.Body || parameter.Schema is null)
            {
                continue;
            }

            var schema = parameter.Schema;
            parameter.Type = schema.Type;
            parameter.Format = schema.Format;
            parameter.Default = schema.Default;
            parameter.MultipleOf = schema.MultipleOf;
            parameter.Maximum = schema.Maximum;
            parameter.ExclusiveMaximum = schema.ExclusiveMaximum;
            parameter.IsExclusiveMaximum = schema.IsExclusiveMaximum;
            parameter.Minimum = schema.Minimum;
            parameter.ExclusiveMinimum = schema.ExclusiveMinimum;
            parameter.IsExclusiveMinimum = schema.IsExclusiveMinimum;
            parameter.MaxLength = schema.MaxLength;
            parameter.MinLength = schema.MinLength;
            parameter.Pattern = schema.Pattern;
            parameter.Item = schema.Item;

            if (schema.IsEnumeration)
            {
                foreach (var value in schema.Enumeration)
                {
                    parameter.Enumeration.Add(value);
                }

                foreach (var name in schema.EnumerationNames)
                {
                    parameter.EnumerationNames.Add(name);
                }
            }

            if (schema.IsNullableRaw == true)
            {
                parameter.ExtensionData ??= new Dictionary<string, object?>();
                parameter.ExtensionData["x-nullable"] = true;
            }

            parameter.Schema = null;
        }
    }

    // Názvy parametrů v dokumentu se tu přepisují na camelCase, aby se
    // Swagger UI shodoval se skutečně přijímanými parametry.
    private static void CamelCaseQueryParameters(NSwag.OpenApiDocument document)
    {
        foreach (var path in document.Paths.Values)
        {
            var parameters = path.Parameters
                .Concat(path.Values.SelectMany(o => o.Parameters));

            foreach (var parameter in parameters
                         .Where(p => p.Kind == OpenApiParameterKind.Query && p.Name.Contains('_')))
            {
                parameter.Name = ToCamelCase(parameter.Name);
            }
        }
    }

    private static string ToCamelCase(string name)
    {
        var builder = new StringBuilder(name.Length);
        var upperNext = false;

        foreach (var c in name)
        {
            if (c == '_')
            {
                upperNext = true;
                continue;
            }

            builder.Append(upperNext ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
            upperNext = false;
        }

        return builder.ToString();
    }
}
