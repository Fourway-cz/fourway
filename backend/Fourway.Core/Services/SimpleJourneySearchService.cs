using Fourway.Core.Infrastructure.Database;
using Fourway.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Fourway.Core.Services;

/// <summary>
/// Jednoduchá implementace <see cref="IJourneySearchService"/>.
/// </summary>
/// <remarks>
/// Neimplementuje pokročilý routovací algoritmus. Vyhledává pouze přímá
/// spojení: jeden <see cref="Trip"/> obsluhující výchozí i cílovou zastávku
/// v požadovaném čase. Přestupy zatím neřeší.
/// </remarks>
public sealed class SimpleJourneySearchService(AppDbContext dbContext) : IJourneySearchService
{
    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Journey>> SearchAsync(
        Stop fromStop,
        Stop toStop,
        DateTime departureTime,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(fromStop);
        ArgumentNullException.ThrowIfNull(toStop);

        if (fromStop.Id == toStop.Id)
        {
            return [];
        }

        // Časy ve StopTime jsou intervaly od půlnoce a mohou přesáhnout 24 hodin,
        // takže spoj sříznutý do následujícího dne může patřit kalendáři předchozího dne.
        var departureDate = DateOnly.FromDateTime(departureTime);
        var serviceDates = new[] { departureDate.AddDays(-1), departureDate };

        var fromStopTimes = await dbContext.StopTimes
            .Where(st => st.StopId == fromStop.Id &&
                         st.StopKind == StopKind.Stops &&
                         (st.DepartureTime != null || st.ArrivalTime != null))
            .ToListAsync(ct);
        var toStopTimes = await dbContext.StopTimes
            .Where(st => st.StopId == toStop.Id &&
                         st.StopKind == StopKind.Stops &&
                         (st.ArrivalTime != null || st.DepartureTime != null))
            .ToListAsync(ct);

        if (fromStopTimes.Count == 0 || toStopTimes.Count == 0)
        {
            return [];
        }

        var tripIds = fromStopTimes.Select(st => st.TripId)
            .Concat(toStopTimes.Select(st => st.TripId))
            .Distinct()
            .ToArray();

        var trips = await dbContext.Trips
            .Where(t => tripIds.Contains(t.Id))
            .ToListAsync(ct);

        var serviceIds = trips.Select(t => t.ServiceId).Distinct().ToArray();
        var services = await dbContext.Services
            .Where(s => serviceIds.Contains(s.Id))
            .Include(s => s.Patterns)
            .Include(s => s.Exceptions)
            .ToListAsync(ct);
        var serviceById = services.ToDictionary(s => s.Id);

        var routeIds = trips.Select(t => t.RouteId).Distinct().ToArray();
        var routes = await dbContext.Routes
            .Where(r => routeIds.Contains(r.Id))
            .ToListAsync(ct);
        var routeById = routes.ToDictionary(r => r.Id);

        var fromByTrip = fromStopTimes
            .GroupBy(st => st.TripId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var toByTrip = toStopTimes
            .GroupBy(st => st.TripId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var matches = new List<(Trip Trip, DateOnly ServiceDate, StopTime From, StopTime To)>();

        foreach (var trip in trips)
        {
            if (!serviceById.TryGetValue(trip.ServiceId, out var service) ||
                !routeById.TryGetValue(trip.RouteId, out var route) ||
                !fromByTrip.TryGetValue(trip.Id, out var tripFrom) ||
                !toByTrip.TryGetValue(trip.Id, out var tripTo))
            {
                continue;
            }

            foreach (var serviceDate in serviceDates)
            {
                if (!route.IsValidOn(serviceDate) || !service.IsActiveOn(serviceDate))
                {
                    continue;
                }

                var dayStart = serviceDate.ToDateTime(TimeOnly.MinValue);

                foreach (var from in tripFrom)
                {
                    var fromTime = from.DepartureTime ?? from.ArrivalTime;
                    if (fromTime is null)
                    {
                        continue;
                    }

                    var absoluteDeparture = dayStart + fromTime.Value;
                    if (absoluteDeparture < departureTime)
                    {
                        continue;
                    }

                    foreach (var to in tripTo)
                    {
                        if (to.StopSequence <= from.StopSequence)
                        {
                            continue;
                        }

                        var toTime = to.ArrivalTime ?? to.DepartureTime;
                        if (toTime is null)
                        {
                            continue;
                        }

                        var absoluteArrival = dayStart + toTime.Value;
                        if (absoluteArrival <= absoluteDeparture)
                        {
                            continue;
                        }

                        matches.Add((trip, serviceDate, from, to));
                    }
                }
            }
        }

        if (matches.Count == 0)
        {
            return [];
        }

        var matchedTripIds = matches.Select(m => m.Trip.Id).Distinct().ToArray();
        var tripStopTimes = await dbContext.StopTimes
            .Where(st => matchedTripIds.Contains(st.TripId))
            .OrderBy(st => st.StopSequence)
            .ToListAsync(ct);

        var stopIds = tripStopTimes.Select(st => st.StopId).Distinct().ToArray();
        var stops = await dbContext.Stops
            .Where(s => stopIds.Contains(s.Id))
            .ToListAsync(ct);
        var stopById = stops.ToDictionary(s => s.Id);

        var matchedRouteIds = matches.Select(m => m.Trip.RouteId).Distinct().ToArray();
        var routeIdsInfos = await dbContext.RouteIdsInfos
            .Where(i => matchedRouteIds.Contains(i.RouteId))
            .ToListAsync(ct);
        var idsInfosByRoute = routeIdsInfos
            .GroupBy(i => i.RouteId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var legStopsByTripAndDate = new Dictionary<(Guid TripId, DateOnly ServiceDate), IReadOnlyList<LegStop>>();
        var journeys = new List<Journey>(matches.Count);

        foreach (var match in matches)
        {
            var key = (match.Trip.Id, match.ServiceDate);
            var dayStart = match.ServiceDate.ToDateTime(TimeOnly.MinValue);
            if (!legStopsByTripAndDate.TryGetValue(key, out var legStops))
            {
                legStops = tripStopTimes
                    .Where(st => st.TripId == match.Trip.Id && stopById.ContainsKey(st.StopId))
                    .Select(st => new LegStop(
                        stopById[st.StopId],
                        st,
                        st.ArrivalTime is { } arrival ? dayStart + arrival : null,
                        st.DepartureTime is { } departure ? dayStart + departure : null))
                    .ToList();
                legStopsByTripAndDate[key] = legStops;
            }

            var route = routeById[match.Trip.RouteId];
            idsInfosByRoute.TryGetValue(route.Id, out var idsInfos);

            var departure = dayStart + (match.From.DepartureTime ?? match.From.ArrivalTime)!.Value;
            var arrival = dayStart + (match.To.ArrivalTime ?? match.To.DepartureTime)!.Value;

            var leg = new Leg(
                match.Trip,
                route,
                idsInfos ?? [],
                fromStop,
                toStop,
                legStops,
                departure,
                arrival);

            journeys.Add(new Journey(fromStop, toStop, [leg]));
        }

        return journeys
            .OrderBy(j => j.DepartureTime)
            .ThenBy(j => j.ArrivalTime)
            .ToList();
    }
}
