using FastEndpoints;
using FluentValidation;
using Fourway.Core.Infrastructure.Database;
using Fourway.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Fourway.Core.Features.Stops;

public record GetStopsRequest(
    [property: QueryParam] int? Limit,
    [property: QueryParam] string? Search);

public class GetStopsValidator : Validator<GetStopsRequest>
{
    public const int DefaultLimit = 10;
    public const int MaxLimit = 50;

    public GetStopsValidator()
    {
        RuleFor(request => request.Limit)
            .InclusiveBetween(1, MaxLimit)
            .When(request => request.Limit.HasValue)
            .WithMessage($"Limit musí být v rozsahu 1 až {MaxLimit}.");

        RuleFor(request => request.Search)
            .MaximumLength(200)
            .WithMessage("Hledaný výraz může mít maximálně 200 znaků.");
    }
}

/// <summary>
/// Vrátí seznam zastávek, volitelně filtrovaný fulltextovým dotazem.
/// </summary>
public class GetStops : Endpoint<GetStopsRequest, IReadOnlyList<StopResponse>>
{
    private readonly AppDbContext _dbContext;

    public GetStops(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/stops");
        Version(1);
        AllowAnonymous();

        Summary(s =>
        {
            s.Summary = "Seznam zastávek";
            s.Description =
                "Vrátí zastávky seřazené podle názvu. Parametr search povolí " +
                "fulltextové vyhledávání v názvu obce, části obce a bližším určení " +
                "zastávky; výsledky jsou řazené podle relevance.";
            s.Response<IReadOnlyList<StopResponse>>(200, "Vyhovující zastávky.");
            s.Response<ErrorResponse>(400, "Neplatný požadavek.");
        });
    }

    public override async Task HandleAsync(GetStopsRequest req, CancellationToken ct)
    {
        var limit = req.Limit ?? GetStopsValidator.DefaultLimit;
        var search = req.Search?.Trim();

        var stops = string.IsNullOrEmpty(search)
            ? await StopsQuery(limit).ToListAsync(ct)
            : await SearchStopsAsync(search, limit, ct);

        await Send.OkAsync(
            stops.Select(s => s.ToStopResponse()).ToList(),
            cancellation: ct);
    }

    private IQueryable<Stop> StopsQuery(int limit) =>
        _dbContext.Stops
            .OrderBy(s => s.MunicipalityNormalized)
            .ThenBy(s => s.MunicipalityPartNormalized)
            .ThenBy(s => s.LocalityDetailNormalized)
            .Take(limit);

    private async Task<List<Stop>> SearchStopsAsync(string search, int limit, CancellationToken ct)
    {
        // Fulltextové vyhledávání nad normalizovanými sloupci (malá písmena,
        // bez diakritiky). Výraz tsvectoru musí souhlasit s indexem
        // ix_stops_search_vector, jinak ho Postgres nepoužije.
        return await _dbContext.Stops
            .FromSqlInterpolated($"""
                SELECT s.*
                FROM stops AS s
                WHERE to_tsvector(
                        'simple',
                        s.municipality_normalized || ' ' ||
                        s.municipality_part_normalized || ' ' ||
                        s.locality_detail_normalized)
                      @@ websearch_to_tsquery('simple', f_unaccent(lower({search})))
                ORDER BY ts_rank(
                        to_tsvector(
                            'simple',
                            s.municipality_normalized || ' ' ||
                            s.municipality_part_normalized || ' ' ||
                            s.locality_detail_normalized),
                        websearch_to_tsquery('simple', f_unaccent(lower({search})))) DESC,
                    s.municipality_normalized,
                    s.municipality_part_normalized,
                    s.locality_detail_normalized
                LIMIT {limit}
                """)
            .ToListAsync(ct);
    }
}
