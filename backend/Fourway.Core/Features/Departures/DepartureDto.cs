using Fourway.Core.Features.Stops;
using Fourway.Core.Models;

namespace Fourway.Core.Features.Departures;

public record DepartureResponse(
    DateTime DepartureTime,
    string RouteNumber,
    string LicenseNumber,
    string? PublicNumber,
    string RouteName,
    int TripNumber,
    TripDirection Direction,
    string? PlatformCode,
    StopResponse Destination);
