using Fourway.Core.Models;
using Fourway.Import.Parsing.Models;

namespace Fourway.Import.Services;

public sealed class CalendarService
{
    public Service BuildService(
        RawSpoj spoj,
        IReadOnlyList<RawCaskod> caskody,
        DateOnly routeValidFrom,
        DateOnly? routeValidTo)
    {
        var service = new Service();
        var defaultEnd = routeValidTo ?? routeValidFrom.AddYears(1);

        var hasWeekdays = spoj.PevneKody.Contains((int)FixedCode.Weekdays);
        var hasSundays = spoj.PevneKody.Contains((int)FixedCode.SundaysAndHolidays);

        var mon = true;
        var tue = true;
        var wed = true;
        var thu = true;
        var fri = true;
        var sat = true;
        var sun = true;

        if (hasWeekdays && !hasSundays)
        {
            sat = false;
            sun = false;
        }
        else if (hasSundays && !hasWeekdays)
        {
            mon = false;
            tue = false;
            wed = false;
            thu = false;
            fri = false;
            sat = false;
            sun = true;
        }

        var parity = WeekParity.All;
        foreach (var ck in caskody)
        {
            if (ck.TypCaskodu == 5)
            {
                parity = WeekParity.Odd;
            }
            else if (ck.TypCaskodu == 6)
            {
                parity = WeekParity.Even;
            }
        }

        var patternAdded = false;
        foreach (var ck in caskody)
        {
            if (ck.TypCaskodu == 1 && ck.DatumOd.HasValue && ck.DatumDo.HasValue)
            {
                service.AddPattern(
                    mon, tue, wed, thu, fri, sat, sun,
                    ck.DatumOd.Value,
                    ck.DatumDo.Value,
                    parity);
                patternAdded = true;
            }
        }

        if (!patternAdded)
        {
            service.AddPattern(
                mon, tue, wed, thu, fri, sat, sun,
                routeValidFrom,
                defaultEnd,
                parity);
        }

        foreach (var ck in caskody)
        {
            if (ck.TypCaskodu == 2 && ck.DatumOd.HasValue)
            {
                AddSafeException(service, ck.DatumOd.Value, ServiceExceptionType.Runs);
            }
            else if (ck.TypCaskodu == 3 && ck.DatumOd.HasValue)
            {
                AddSafeException(service, ck.DatumOd.Value, ServiceExceptionType.DoesNotRun);
            }
            else if (ck.TypCaskodu == 4 && ck.DatumOd.HasValue && ck.DatumDo.HasValue)
            {
                var cur = ck.DatumOd.Value;
                var end = ck.DatumDo.Value;
                if (end.DayNumber - cur.DayNumber <= 62)
                {
                    while (cur <= end)
                    {
                        AddSafeException(service, cur, ServiceExceptionType.DoesNotRun);
                        cur = cur.AddDays(1);
                    }
                }
            }
        }

        service.RecomputeRuleHash();
        return service;
    }

    private static void AddSafeException(Service service, DateOnly date, ServiceExceptionType type)
    {
        if (service.Exceptions.All(x => x.Date != date))
        {
            service.AddException(date, type);
        }
    }
}
