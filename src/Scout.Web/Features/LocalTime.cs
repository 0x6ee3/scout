using System.ComponentModel.DataAnnotations;

namespace Scout.Web.Features;

public static class LocalTime
{
    public static DateTime? ToUtc(DateTime? wallTime, int? offsetMinutes = null, TimeZoneInfo? zone = null)
    {
        if (wallTime is null)
            return null;
        zone ??= TimeZoneInfo.Local;
        var local = DateTime.SpecifyKind(wallTime.Value, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(local))
            throw new ValidationException("This local time does not exist because the clock changes. Choose another time.");
        if (zone.IsAmbiguousTime(local))
        {
            var offsets = zone.GetAmbiguousTimeOffsets(local);
            if (!offsetMinutes.HasValue || !offsets.Contains(TimeSpan.FromMinutes(offsetMinutes.Value)))
                throw new ValidationException("This time occurs twice. Choose its UTC offset.");
            return new DateTimeOffset(local, TimeSpan.FromMinutes(offsetMinutes.Value)).UtcDateTime;
        }
        return TimeZoneInfo.ConvertTimeToUtc(local, zone);
    }
}
