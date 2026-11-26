namespace Fourway.Core.Features.Stops;

public record StopResponse(
    Guid Id,
    string Name,
    string Municipality,
    string? MunicipalityPart,
    string? LocalityDetail,
    bool HasPlatform,
    bool WheelchairAccessible,
    bool HasShelter,
    bool HasInfoBoard,
    bool HasLighting,
    double? Latitude,
    double? Longitude);
