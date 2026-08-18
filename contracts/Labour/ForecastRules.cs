using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

/// <summary>
/// Pure forecast rules for the Labour overview (scope §5). Live in contracts, like LabourRules,
/// so the test project can exercise them without the API host.
///
/// The method statement, kept nearly verbatim from the reference build: contracted days
/// multiplied by day rate, less every day recorded as holiday, a half day or not worked; days
/// not yet submitted stay at the full rate, so the figure is only as accurate as the submission
/// rate (which is what the confidence bar reports).
/// </summary>
public static class ForecastRules
{
    public const decimal StandardHoursPerDay = 8.0m;

    /// <summary>Mon–Fri days in the month.</summary>
    public static int WorkingDaysInMonth(int year, int month)
    {
        var days = DateTime.DaysInMonth(year, month);
        var count = 0;
        for (var day = 1; day <= days; day++)
        {
            var dow = new DateTime(year, month, day).DayOfWeek;
            if (dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday) count++;
        }
        return count;
    }

    /// <summary>Mon–Fri days in the month up to and including <paramref name="today"/>
    /// (0 when the month is entirely in the future, the full count when it is past).</summary>
    public static int ElapsedWorkingDays(int year, int month, DateTime today)
    {
        var first = new DateTime(year, month, 1);
        if (today < first) return 0;
        var last = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        var upTo = today < last ? today : last;
        var count = 0;
        for (var date = first; date <= upTo; date = date.AddDays(1))
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday) count++;
        return count;
    }

    /// <summary>How many days a recorded absence deducts from the projection.</summary>
    public static decimal AbsenceDeductionDays(AbsenceKind kind) => kind switch
    {
        AbsenceKind.HalfDay => 0.5m,
        _ => 1.0m,
    };

    /// <summary>
    /// Projected month cost for one worker: contracted days × day rate, less absence deductions
    /// at the day rate. Recorded worked days do not change the projection — they confirm it.
    /// Deductions never take the projection below zero.
    /// </summary>
    public static decimal ProjectedCost(decimal contractedDays, decimal dayRate, IEnumerable<AbsenceKind> absences)
    {
        var deduction = absences.Sum(AbsenceDeductionDays);
        var projectedDays = Math.Max(0m, contractedDays - deduction);
        return decimal.Round(projectedDays * dayRate, 2);
    }

    /// <summary>The £ deducted by absences (the "time off logged" header figure).</summary>
    public static decimal TimeOffCost(decimal dayRate, IEnumerable<AbsenceKind> absences) =>
        decimal.Round(absences.Sum(AbsenceDeductionDays) * dayRate, 2);

    /// <summary>
    /// Net payable after CIS. The deduction applies to labour only — materials and travel
    /// settlement lines are added outside this method and never deducted.
    /// </summary>
    public static decimal AmountDue(decimal projectedLabourCost, decimal cisRatePercent) =>
        decimal.Round(projectedLabourCost * (1m - cisRatePercent / 100m), 2);

    /// <summary>Hours recorded → days at the 8-hour standard day.</summary>
    public static decimal HoursToDays(decimal hours) => decimal.Round(hours / StandardHoursPerDay, 2);

    /// <summary>The Monday of the week containing <paramref name="date"/> (UTC date arithmetic).</summary>
    public static DateTime WeekStartOf(DateTime date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7; // Monday = 0
        return date.Date.AddDays(-offset);
    }

    /// <summary>
    /// A week is signable only when every elapsed Mon–Fri day in it (within the month, up to
    /// today) is confirmed: an approved timesheet, a rejected-with-reason timesheet, or a
    /// recorded absence. Days with only Submitted timesheets are not yet signable.
    /// </summary>
    public static bool WeekIsSignable(
        DateTime weekStart, DateTime today,
        IReadOnlySet<DateTime> approvedOrRejectedDays,
        IReadOnlySet<DateTime> absenceDays)
    {
        for (var i = 0; i < 5; i++)
        {
            var day = weekStart.AddDays(i);
            if (day > today) break;
            if (!approvedOrRejectedDays.Contains(day) && !absenceDays.Contains(day)) return false;
        }
        return true;
    }
}
