using Fourway.Core.Models;

namespace Fourway.Core.Features.Stops;

internal static class StopResponseExtensions
{
    public static StopResponse ToStopResponse(this Stop stop)
    {
        var hasLocation = stop.HasLocation;

        return new StopResponse(
            stop.Id,
            stop.DisplayName,
            stop.Municipality,
            stop.MunicipalityPart,
            stop.LocalityDetail,
            stop.HasPlatform,
            stop.WheelchairAccessible,
            stop.HasShelter,
            stop.HasInfoBoard,
            stop.HasLighting,
            hasLocation ? stop.Location!.Y : null,
            hasLocation ? stop.Location!.X : null);
    }
}
