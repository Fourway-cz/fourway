using FastEndpoints;
using FluentValidation;
using Fourway.Core.Features.Stops;
using Fourway.Core.Infrastructure.Database;
using Fourway.Core.Models;
using Fourway.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Fourway.Core.Features.Journeys;

public record GetJourneysRequest(
    [property: QueryParam] Guid FromStopId,
    [property: QueryParam] Guid ToStopId,
    [property: QueryParam] DateTime DepartureTime);

public class GetJourneysValidator : Validator<GetJourneysRequest>
{
    public GetJourneysValidator()
    {
        RuleFor(request => request.FromStopId)
            .NotEmpty()
            .WithMessage("ID výchozí a cílové zastávky jsou povinné.");

        RuleFor(request => request.ToStopId)
            .NotEmpty()
            .WithMessage("ID výchozí a cílové zastávky jsou povinné.");

        RuleFor(request => request.DepartureTime)
            .NotEmpty()
            .WithMessage("Čas odjezdu je povinný.");

        RuleFor(request => request.ToStopId)
            .NotEqual(request => request.FromStopId)
            .When(request => request.FromStopId != Guid.Empty && request.ToStopId != Guid.Empty)
            .WithMessage("Výchozí a cílová zastávka musí být různé.");
    }
}

public record GetJourneysResponse(
    StopResponse FromStop,
    StopResponse ToStop,
    IReadOnlyList<JourneyResponse> Journeys);

/// <summary>
/// Vyhledá přímá spojení mezi dvěma zastávkami.
/// </summary>
public class GetJourneys : Endpoint<GetJourneysRequest, GetJourneysResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly IJourneySearchService _journeySearchService;

    public GetJourneys(AppDbContext dbContext, IJourneySearchService journeySearchService)
    {
        _dbContext = dbContext;
        _journeySearchService = journeySearchService;
    }

    public override void Configure()
    {
        Get("/journeys");
        Version(1);
        AllowAnonymous();

        Summary(s =>
        {
            s.Summary = "Vyhledání spojení mezi zastávkami";
            s.Description =
                "Najde přímá spojení z výchozí do cílové zastávky, která odjíždějí " +
                "nejdříve v zadaném čase. Přestupy zatím nejsou podporovány.";
            s.Response<GetJourneysResponse>(200, "Nalezená spojení, případně prázdný seznam.");
            s.Response<ErrorResponse>(400, "Neplatný požadavek.");
            s.Response<ErrorResponse>(404, "Výchozí nebo cílová zastávka nebyla nalezena.");
        });
    }

    public override async Task HandleAsync(GetJourneysRequest req, CancellationToken ct)
    {
        var fromStop = await _dbContext.Stops
            .FirstOrDefaultAsync(s => s.Id == req.FromStopId, ct);
        var toStop = await _dbContext.Stops
            .FirstOrDefaultAsync(s => s.Id == req.ToStopId, ct);

        if (fromStop is null || toStop is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var journeys = await _journeySearchService.SearchAsync(
            fromStop,
            toStop,
            req.DepartureTime,
            ct);

        var response = new GetJourneysResponse(
            fromStop.ToStopResponse(),
            toStop.ToStopResponse(),
            journeys.Select(ToJourneyResponse).ToList());

        await Send.OkAsync(response, cancellation: ct);
    }

    private static JourneyResponse ToJourneyResponse(Journey journey) => new(
        journey.DepartureTime,
        journey.ArrivalTime,
        journey.Duration,
        journey.Legs.Select(ToLegResponse).ToList());

    private static FullJourneyLegResponse ToLegResponse(Leg leg)
    {
        var idsInfo = leg.IdsInfos.FirstOrDefault(i => i.FullyIntegrated)
            ?? leg.IdsInfos.FirstOrDefault();

        return new FullJourneyLegResponse(
            idsInfo?.PublicNumber ?? leg.Route.LicenseNumber,
            leg.Route.LicenseNumber,
            idsInfo?.PublicNumber,
            leg.Route.Name,
            leg.DepartureTime,
            leg.ArrivalTime,
            leg.Duration,
            leg.Stops.Select(ToLegStopResponse).ToList());
    }

    private static JourneyLegStopResponse ToLegStopResponse(LegStop legStop) => new(
        legStop.Stop.ToStopResponse(),
        legStop.StopTime.StopSequence,
        legStop.StopTime.StopKind,
        legStop.StopTime.PlatformCode,
        legStop.ArrivalTime,
        legStop.DepartureTime);
}
