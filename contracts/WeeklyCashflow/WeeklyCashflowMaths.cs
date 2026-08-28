using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.WeeklyCashflow;

// ============================================================================
// The Weekly Cashflow's arithmetic — pure maths, no EF/HTTP, unit-tested
// directly (WeeklyCashflowMathsTests). It takes the Xero-fed entries (each
// outstanding bill and sales invoice, as the aged views already read them),
// the manual items and the accountant's placements, and answers one question:
// WHICH WEEK does each pound move in?
//
// Honesty rules, structural not stylistic:
//   * Weeks are Monday-anchored, midnight UTC. The axis is the current week
//     plus the weeks after it; dated flows beyond it fold into a Later bucket
//     rather than dropping off.
//   * Overdue lands in the current week — never a past week, never dropped.
//     An entry with no date at all is money owed now: current week.
//   * A placement is the accountant's word and always wins — clamped into the
//     axis (a stale placement in a past week means "pay now": current week).
//   * A recurring item's occurrences exist only inside the visible axis, and
//     occurrences before the current week are assumed dealt with — a wage run
//     from three weeks ago is not still queued. A ONE-OFF item, by contrast,
//     stays in the current week however old it is, until it is archived —
//     it exists precisely because the money is still to be paid.
//   * Nothing is netted away: every entry that goes in comes out in exactly
//     one bucket, so the grid's totals always explain the inputs.
// ============================================================================

/// <summary>The grid's row bands, in render order — the first is cash in, the rest cash out.
/// Xero seeds the first two; the manual categories fill the rest.</summary>
public enum WeeklyCashflowBand
{
    ClientReceipts,
    SupplierBills,
    Subcontractors,
    Staff,
    Subscriptions,
    Other,
    DirectDebits
}

/// <summary>One Xero-fed movable entry, as the page maps it from the aged snapshots: a supplier
/// bill (or purchase credit note — negative Amount) or an outstanding sales invoice. DueOn null
/// = no date on the document: owed now. ExpectedOn is the accountant's expected/planned payment
/// date set in Xero — when present it, not DueOn, decides the entry's natural week (retention
/// held back, an agreed late payment); DueOn stays what the document says is owed.</summary>
public sealed record WeeklyCashflowSeed(
    string PlacementKey,
    WeeklyCashflowBand Band,
    string Label,
    string? Detail,
    decimal Amount,
    DateTimeOffset? DueOn,
    DateTimeOffset? ExpectedOn = null);

/// <summary>One entry placed on the grid. WeekIndex is 0-based into the week axis, or
/// <see cref="WeeklyCashflowView.LaterIndex"/> for the Later bucket. Moved marks a placement in
/// force — the accountant's week, not the document's. ItemId is set for manual entries so the
/// row can open its item for editing. ExpectedOn carries the Xero expected/planned payment date
/// when one is set; <see cref="NaturalOn"/> is the week the entry seeds into (and ↺ returns to)
/// — expected first, due date otherwise.</summary>
public sealed record WeeklyCashflowEntry(
    string PlacementKey,
    WeeklyCashflowBand Band,
    string Label,
    string? Detail,
    decimal Amount,
    DateTimeOffset? NaturalDueOn,
    int WeekIndex,
    bool Moved,
    string? ItemId = null,
    DateTimeOffset? ExpectedOn = null)
{
    public DateTimeOffset? NaturalOn => ExpectedOn ?? NaturalDueOn;
}

/// <summary>
/// The built grid: the week axis (Mondays, midnight UTC), every entry with its week, and the
/// per-week totals. The totals arrays run one slot past the axis — the last slot is the Later
/// bucket. Closing is the running bank balance per VISIBLE week (Later excluded until it
/// arrives), present only when an opening balance was supplied — it is the directors' line.
/// </summary>
public sealed record WeeklyCashflowView(
    IReadOnlyList<DateTimeOffset> WeekStarts,
    IReadOnlyList<WeeklyCashflowEntry> Entries,
    IReadOnlyList<decimal> CashIn,
    IReadOnlyList<decimal> CashOut,
    IReadOnlyList<decimal> Net,
    IReadOnlyList<decimal>? Closing)
{
    public int LaterIndex => WeekStarts.Count;

    /// <summary>The week the balance bottoms out — the number this page exists to surface.
    /// -1 when there is no balance line.</summary>
    public int MinClosingIndex
    {
        get
        {
            if (Closing is null || Closing.Count == 0) return -1;
            var minIndex = 0;
            for (var index = 1; index < Closing.Count; index++)
                if (Closing[index] < Closing[minIndex]) minIndex = index;
            return minIndex;
        }
    }
}

public static class WeeklyCashflowMaths
{
    /// <summary>The standard 13-week cashflow window.</summary>
    public const int DefaultWeekCount = 13;

    /// <summary>The Monday of the given date's week, midnight UTC.</summary>
    public static DateTimeOffset WeekStartFor(DateTimeOffset date)
    {
        var day = date.UtcDateTime.Date;
        var daysPastMonday = ((int)day.DayOfWeek + 6) % 7; // Monday 0 … Sunday 6
        return new DateTimeOffset(day.AddDays(-daysPastMonday), TimeSpan.Zero);
    }

    /// <summary>The visible axis: <paramref name="weekCount"/> Mondays from the current week.</summary>
    public static IReadOnlyList<DateTimeOffset> BuildWeeks(DateTimeOffset today, int weekCount = DefaultWeekCount)
    {
        var first = WeekStartFor(today);
        var weeks = new DateTimeOffset[weekCount];
        for (var index = 0; index < weekCount; index++) weeks[index] = first.AddDays(index * 7);
        return weeks;
    }

    // ---- The stable placement keys — the one vocabulary the page, the placements table and
    // this engine share. Change these and every stored placement goes stale, so don't. ----

    public static string BillKeyFor(string xeroInvoiceId) => $"bill:{xeroInvoiceId}";

    public static string ReceiptKeyFor(string xeroInvoiceId) => $"receipt:{xeroInvoiceId}";

    /// <summary>A manual occurrence's key carries its NATURAL date, so a recurring item's weeks
    /// move independently and an edit that shifts the schedule releases the old placements.</summary>
    public static string ManualKeyFor(string weeklyCashflowItemId, DateTimeOffset naturalDueOn) =>
        $"manual:{weeklyCashflowItemId}:{naturalDueOn.UtcDateTime:yyyy-MM-dd}";

    /// <summary>Which band a manual category's items render under.</summary>
    public static WeeklyCashflowBand BandFor(WeeklyCashflowCategory category) => category switch
    {
        WeeklyCashflowCategory.Subcontractor => WeeklyCashflowBand.Subcontractors,
        WeeklyCashflowCategory.Staff => WeeklyCashflowBand.Staff,
        WeeklyCashflowCategory.Subscription => WeeklyCashflowBand.Subscriptions,
        WeeklyCashflowCategory.DirectDebit => WeeklyCashflowBand.DirectDebits,
        _ => WeeklyCashflowBand.Other
    };

    public static bool IsCashIn(WeeklyCashflowBand band) => band == WeeklyCashflowBand.ClientReceipts;

    /// <summary>
    /// Builds the whole grid. <paramref name="seeds"/> are the Xero-fed entries as the page maps
    /// them; <paramref name="items"/> and <paramref name="placements"/> are the stored plan;
    /// <paramref name="openingBalance"/> is the Xero bank position (directors) or null, which
    /// omits the balance line entirely.
    /// </summary>
    public static WeeklyCashflowView Build(
        DateTimeOffset today,
        IReadOnlyList<WeeklyCashflowSeed> seeds,
        IReadOnlyList<WeeklyCashflowItem> items,
        IReadOnlyList<WeeklyCashflowPlacement> placements,
        decimal? openingBalance,
        int weekCount = DefaultWeekCount)
    {
        var weeks = BuildWeeks(today, weekCount);
        var axisStart = weeks[0];
        var axisEnd = weeks[^1].AddDays(7);
        var laterIndex = weeks.Count;

        var plannedWeekByKey = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
        foreach (var placement in placements)
            plannedWeekByKey[placement.PlacementKey] = placement.PlannedWeekStart;

        int IndexOfWeek(DateTimeOffset week) =>
            week < axisStart ? 0
            : week >= axisEnd ? laterIndex
            : (int)((WeekStartFor(week) - axisStart).TotalDays / 7);

        (int WeekIndex, bool Moved) Place(string key, DateTimeOffset? naturalDueOn)
        {
            if (plannedWeekByKey.TryGetValue(key, out var planned))
                return (IndexOfWeek(planned), true);
            if (naturalDueOn is not { } due) return (0, false); // no honest date — owed now
            return (IndexOfWeek(WeekStartFor(due)), false);
        }

        var entries = new List<WeeklyCashflowEntry>();

        foreach (var seed in seeds)
        {
            // The natural week honours the accountant's Xero expected/planned payment date
            // when one is set — the due date is what's owed, the expected date is when the
            // money will really move (retention held back, an agreed late payment).
            var (weekIndex, moved) = Place(seed.PlacementKey, seed.ExpectedOn ?? seed.DueOn);
            entries.Add(new WeeklyCashflowEntry(
                seed.PlacementKey, seed.Band, seed.Label, seed.Detail, seed.Amount,
                seed.DueOn, weekIndex, moved, null, seed.ExpectedOn));
        }

        foreach (var item in items)
        {
            if (item.ArchivedAt is not null) continue;
            foreach (var occurrence in OccurrencesFor(item, axisStart, axisEnd))
            {
                var key = ManualKeyFor(item.WeeklyCashflowItemId, occurrence);
                var (weekIndex, moved) = Place(key, occurrence);
                entries.Add(new WeeklyCashflowEntry(
                    key, BandFor(item.Category), item.Name,
                    WeeklyCashflowRecurrences.Label(item.Recurrence),
                    item.Amount, occurrence, weekIndex, moved, item.WeeklyCashflowItemId));
            }
        }

        var cashIn = new decimal[weekCount + 1];
        var cashOut = new decimal[weekCount + 1];
        foreach (var entry in entries)
        {
            if (IsCashIn(entry.Band)) cashIn[entry.WeekIndex] += entry.Amount;
            else cashOut[entry.WeekIndex] += entry.Amount;
        }

        var net = new decimal[weekCount + 1];
        for (var index = 0; index <= weekCount; index++) net[index] = cashIn[index] - cashOut[index];

        decimal[]? closing = null;
        if (openingBalance is { } opening)
        {
            closing = new decimal[weekCount];
            var running = opening;
            for (var index = 0; index < weekCount; index++)
            {
                running += net[index];
                closing[index] = running;
            }
        }

        return new WeeklyCashflowView(weeks, entries, cashIn, cashOut, net, closing);
    }

    /// <summary>
    /// A manual item's occurrence dates inside the axis. One-offs keep their date however old
    /// (overdue → the current week, above); recurring occurrences exist only from the current
    /// week to the axis end — past ones are assumed dealt with, and an open-ended schedule
    /// doesn't flood the Later bucket.
    /// </summary>
    public static IEnumerable<DateTimeOffset> OccurrencesFor(
        WeeklyCashflowItem item, DateTimeOffset axisStart, DateTimeOffset axisEnd)
    {
        var first = new DateTimeOffset(item.FirstDueOn.UtcDateTime.Date, TimeSpan.Zero);
        var last = item.LastDueOn is { } lastDueOn
            ? new DateTimeOffset(lastDueOn.UtcDateTime.Date, TimeSpan.Zero)
            : (DateTimeOffset?)null;

        switch (item.Recurrence)
        {
            case WeeklyCashflowRecurrence.OneOff:
                // Overdue → the current week; beyond the horizon → Later. Both are Place()'s
                // job — the occurrence itself always exists.
                yield return first;
                yield break;

            case WeeklyCashflowRecurrence.Weekly:
            {
                var occurrence = first;
                if (occurrence < axisStart)
                {
                    var weeksBehind = (int)Math.Ceiling((axisStart - occurrence).TotalDays / 7d);
                    occurrence = occurrence.AddDays(weeksBehind * 7);
                }
                while (occurrence < axisEnd && (last is null || occurrence <= last))
                {
                    yield return occurrence;
                    occurrence = occurrence.AddDays(7);
                }
                yield break;
            }

            case WeeklyCashflowRecurrence.Monthly:
            {
                // The anchor day of the month, clamped into shorter months (31st → 30 Apr, 28 Feb).
                var anchorDay = first.Day;
                var month = new DateTime(first.Year, first.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                while (true)
                {
                    var day = Math.Min(anchorDay, DateTime.DaysInMonth(month.Year, month.Month));
                    var occurrence = new DateTimeOffset(month.AddDays(day - 1), TimeSpan.Zero);
                    if (occurrence >= axisEnd) yield break;
                    if (last is { } lastDate && occurrence > lastDate) yield break;
                    if (occurrence >= axisStart && occurrence >= first) yield return occurrence;
                    month = month.AddMonths(1);
                }
            }

            default:
                yield break;
        }
    }
}
