using Fourway.Core.Features.Stops;
using Fourway.Core.Models;

namespace Fourway.Core.Features.Journeys;

public record JourneyResponse(
    DateTime DepartureTime,
    DateTime ArrivalTime,
    TimeSpan Duration,
    IReadOnlyList<FullJourneyLegResponse> Legs);

public record JourneyLegResponse(
    string RouteNumber,
    string LicenseNumber,
    string? PublicNumber,
    string RouteName,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    TimeSpan Duration);

public record FullJourneyLegResponse(
    string RouteNumber,
    string LicenseNumber,
    string? PublicNumber,
    string RouteName,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    TimeSpan Duration,
    IReadOnlyList<JourneyLegStopResponse> Stops)
    : JourneyLegResponse(RouteNumber, LicenseNumber, PublicNumber, RouteName, DepartureTime, ArrivalTime, Duration);

public record JourneyLegStopResponse(
    StopResponse Stop,
    int StopSequence,
    StopKind StopKind,
    string? PlatformCode,
    DateTime? ArrivalTime,
    DateTime? DepartureTime);