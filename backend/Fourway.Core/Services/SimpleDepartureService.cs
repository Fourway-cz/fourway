using Fourway.Core.Infrastructure.Database;
using Fourway.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Fourway.Core.Services;

/// <summary>
/// Jednoduchá implementace <see cref="IDepartureService"/>.
/// </summary>
/// <remarks>
/// Projde všechny <see cref="StopTime"/> dané zastávky, ověří platnost linky
/// a kalendáře spoje v daném dni a vrátí odjezdy seřazené podle času.
/// Stejně jako <see cref="SimpleJourneySearchService"/> počítá s časy, které
/// mohou přesáhnout 24 hodin, takže kontroluje i kalendář předchozího dne.
/// </remarks>
public sealed class SimpleDepartureService(AppDbContext dbContext) : IDepartureService
{
    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Departure>> SearchAsync(
        Stop stop,
        DateTime departureTime,
        int limit,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stop);

        // Časy ve StopTime jsou intervaly od půlnoce a mohou přesáhnout 24 hodin,
        // takže spoj sříznutý do následujícího dne může patřit kalendáři předchozího dne.
        var departureDate = DateOnly.FromDateTime(departureTime);
        var serviceDates = new[] { departureDate.AddDays(-1), departureDate };

        var stopTimes = await dbContext.StopTimes
            .Where(st => st.StopId == stop.Id &&
                         st.StopKind == StopKind.Stops &&
                         st.DepartureTime != null)
            .ToListAsync(ct);

        if (stopTimes.Count == 0)
        {
            return [];
        }

        var tripIds = stopTimes.Select(st => st.TripId).Distinct().ToArray();

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

        var routeIdsInfos = await dbContext.RouteIdsInfos
            .Where(i => routeIds.Contains(i.RouteId))
            .ToListAsync(ct);
        var idsInfosByRoute = routeIdsInfos
            .GroupBy(i => i.RouteId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<RouteIdsInfo>)g.ToList());

        var stopTimesByTrip = stopTimes
            .GroupBy(st => st.TripId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var matches = new List<(Trip Trip, StopTime StopTime, DateTime Departure)>();

        foreach (var trip in trips)
        {
            if (!serviceById.TryGetValue(trip.ServiceId, out var service) ||
                !routeById.TryGetValue(trip.RouteId, out var route) ||
                !stopTimesByTrip.TryGetValue(trip.Id, out var tripStopTimes))
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

                foreach (var stopTime in tripStopTimes)
                {
                    var absoluteDeparture = dayStart + stopTime.DepartureTime!.Value;
                    if (absoluteDeparture >= departureTime)
                    {
                        matches.Add((trip, stopTime, absoluteDeparture));
                    }
                }
            }
        }

        var departures = matches
            .OrderBy(m => m.Departure)
            .ThenBy(m => m.Trip.TripNumber)
            .Take(limit)
            .ToList();

        if (departures.Count == 0)
        {
            return [];
        }

        // Cílová zastávka je poslední zastávka spoje podle pořadí.
        var matchedTripIds = departures.Select(d => d.Trip.Id).Distinct().ToArray();
        var terminusStopIds = (await dbContext.StopTimes
                .Where(st => matchedTripIds.Contains(st.TripId))
                .ToListAsync(ct))
            .GroupBy(st => st.TripId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(st => st.StopSequence).First().StopId);

        var destinationIds = terminusStopIds.Values.Distinct().ToArray();
        var destinationStops = await dbContext.Stops
            .Where(s => destinationIds.Contains(s.Id))
            .ToListAsync(ct);
        var destinationById = destinationStops.ToDictionary(s => s.Id);

        return departures
            .Select(m =>
            {
                var route = routeById[m.Trip.RouteId];
                idsInfosByRoute.TryGetValue(route.Id, out var idsInfos);

                return new Departure(
                    m.Trip,
                    route,
                    idsInfos ?? [],
                    m.StopTime,
                    destinationById[terminusStopIds[m.Trip.Id]],
                    m.Departure);
            })
            .ToList();
    }
}
