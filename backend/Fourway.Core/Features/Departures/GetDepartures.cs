using FastEndpoints;
using FluentValidation;
using Fourway.Core.Features.Stops;
using Fourway.Core.Infrastructure.Database;
using Fourway.Core.Models;
using Fourway.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Fourway.Core.Features.Departures;

public record GetDeparturesRequest(
    [property: QueryParam] Guid StopId,
    [property: QueryParam] DateTime DepartureTime,
    [property: QueryParam] int? Limit);

public class GetDeparturesValidator : Validator<GetDeparturesRequest>
{
    public GetDeparturesValidator()
    {
        RuleFor(request => request.StopId)
            .NotEmpty()
            .WithMessage("ID zastávky je povinné.");

        RuleFor(request => request.DepartureTime)
            .NotEmpty()
            .WithMessage("Čas odjezdu je povinný.");

        RuleFor(request => request.Limit)
            .InclusiveBetween(1, GetDepartures.MaxDeparturesLimit)
            .When(request => request.Limit.HasValue)
            .WithMessage($"Limit musí být mezi 1 a {GetDepartures.MaxDeparturesLimit}.");
    }
}

public record GetDeparturesResponse(
    StopResponse Stop,
    IReadOnlyList<DepartureResponse> Departures);

/// <summary>
/// Vyhledá spoje odjíždějící ze zadané zastávky.
/// </summary>
public class GetDepartures : Endpoint<GetDeparturesRequest, GetDeparturesResponse>
{
    /// <summary>Výchozí počet odjezdů, když klient limit nespecifikuje.</summary>
    public const int DefaultDeparturesLimit = 20;

    /// <summary>Nejvyšší povolený počet odjezdů v jednom požadavku.</summary>
    public const int MaxDeparturesLimit = 50;

    private readonly AppDbContext _dbContext;
    private readonly IDepartureService _departureService;

    public GetDepartures(AppDbContext dbContext, IDepartureService departureService)
    {
        _dbContext = dbContext;
        _departureService = departureService;
    }

    public override void Configure()
    {
        Get("/departures");
        Version(1);
        AllowAnonymous();

        Summary(s =>
        {
            s.Summary = "Odjezdy ze zastávky";
            s.Description =
                "Najde spoje, které ze zadané zastávky odjíždějí nejdříve v zadaném čase. " +
                $"Vrací se nejvýše {MaxDeparturesLimit} odjezdů, výchozí limit je {DefaultDeparturesLimit}.";
            s.Response<GetDeparturesResponse>(200, "Nalezené odjezdy, případně prázdný seznam.");
            s.Response<ErrorResponse>(400, "Neplatný požadavek.");
            s.Response<ErrorResponse>(404, "Zastávka nebyla nalezena.");
        });
    }

    public override async Task HandleAsync(GetDeparturesRequest req, CancellationToken ct)
    {
        var stop = await _dbContext.Stops
            .FirstOrDefaultAsync(s => s.Id == req.StopId, ct);

        if (stop is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var departures = await _departureService.SearchAsync(
            stop,
            req.DepartureTime,
            Math.Min(req.Limit ?? DefaultDeparturesLimit, MaxDeparturesLimit),
            ct);

        var response = new GetDeparturesResponse(
            stop.ToStopResponse(),
            departures.Select(ToDepartureResponse).ToList());

        await Send.OkAsync(response, cancellation: ct);
    }

    private static DepartureResponse ToDepartureResponse(Departure departure)
    {
        var idsInfo = departure.IdsInfos.FirstOrDefault(i => i.FullyIntegrated)
            ?? departure.IdsInfos.FirstOrDefault();

        return new DepartureResponse(
            departure.DepartureTime,
            idsInfo?.PublicNumber ?? departure.Route.LicenseNumber,
            departure.Route.LicenseNumber,
            idsInfo?.PublicNumber,
            departure.Route.Name,
            departure.Trip.TripNumber,
            departure.Trip.Direction,
            departure.StopTime.PlatformCode,
            departure.Destination.ToStopResponse());
    }
}
