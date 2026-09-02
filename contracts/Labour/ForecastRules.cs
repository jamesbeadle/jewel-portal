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

    /// <summary>The first of the month containing <paramref name="date"/>.</summary>
    public static DateTime MonthStartOf(DateTime date) => new(date.Year, date.Month, 1);

    // ---- Week parts -----------------------------------------------------------------------------
    // Sign-off is per worker, per week AND per month (2026-09-02, the accountant's ask). A week
    // that straddles a month end — Mon 31 Aug to Sun 6 Sep — is two parts: "to 31 Aug", which
    // August's month-end needs, and "from 1 Sep", which September's does. Before the split the
    // old month could not close until the whole week had elapsed and every September day in it
    // was approved, so August's Xero run waited for the 6th. A week inside one month is one part.

    /// <summary>True when the week beginning <paramref name="weekStart"/> has days in more than
    /// one month — the Sunday falls in a later month than the Monday.</summary>
    public static bool WeekStraddlesMonthEnd(DateTime weekStart) =>
        MonthStartOf(weekStart.AddDays(6)) != MonthStartOf(weekStart);

    /// <summary>True when the week beginning <paramref name="weekStart"/> has at least one day
    /// (Mon–Sun) in the month beginning <paramref name="monthStart"/>.</summary>
    public static bool WeekTouchesMonth(DateTime weekStart, DateTime monthStart) =>
        weekStart.Date.AddDays(6) >= monthStart.Date && weekStart.Date < monthStart.Date.AddMonths(1);

    /// <summary>The first and last calendar day of the week's part inside the month — the whole
    /// week when it lies within the month.</summary>
    public static (DateTime First, DateTime Last) WeekPart(DateTime weekStart, DateTime monthStart)
    {
        var monthEnd = monthStart.Date.AddMonths(1).AddDays(-1);
        var first = weekStart.Date > monthStart.Date ? weekStart.Date : monthStart.Date;
        var weekEnd = weekStart.Date.AddDays(6);
        var last = weekEnd < monthEnd ? weekEnd : monthEnd;
        return (first, last);
    }

    /// <summary>
    /// A week is signable only when every elapsed Mon–Fri day in it (up to today) is confirmed:
    /// an approved timesheet, a rejected-with-reason timesheet, or a recorded absence. Days with
    /// only Submitted timesheets are not yet signable. This is the whole-week rule; a month's
    /// sign-off uses <see cref="WeekPartIsSignable"/>, which looks only at the days inside it.
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

    /// <summary>
    /// The signable rule for one month's part of a week: every elapsed Mon–Fri day of the week
    /// that falls INSIDE the month (up to today) is confirmed. Days of the same week in the
    /// neighbouring month are that month's business — so "to 31 Aug" can be signed on 1 Sep with
    /// only Monday approved, and September's days in the week never hold August up.
    /// </summary>
    public static bool WeekPartIsSignable(
        DateTime weekStart, DateTime monthStart, DateTime today,
        IReadOnlySet<DateTime> approvedOrRejectedDays,
        IReadOnlySet<DateTime> absenceDays)
    {
        var (first, last) = WeekPart(weekStart, monthStart);
        for (var i = 0; i < 5; i++)
        {
            var day = weekStart.AddDays(i);
            if (day > today) break;
            if (day < first || day > last) continue;
            if (!approvedOrRejectedDays.Contains(day) && !absenceDays.Contains(day)) return false;
        }
        return true;
    }
}
