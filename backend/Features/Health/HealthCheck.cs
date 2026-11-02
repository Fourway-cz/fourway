using FastEndpoints;

namespace Fourway.Api.Features.Health;

public record HealthCheckResponse(string Status);

public class HealthCheck : EndpointWithoutRequest<HealthCheckResponse>
{
    public override void Configure()
    {
        Get("/health");
        Version(1);
        AllowAnonymous();

        Summary(s =>
        {
            s.Summary = "Health check";
            s.Description = "Vrátí status ok, pokud API běží.";
            s.Response<HealthCheckResponse>(200, "Status je OK");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(
            new HealthCheckResponse("ok"),
            cancellation: ct);
    }
}
