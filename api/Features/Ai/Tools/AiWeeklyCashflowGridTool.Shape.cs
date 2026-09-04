using System.Globalization;
using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Contracts.WeeklyCashflow.Export;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>How the grid reads as JSON: the week axis once, then each band with its lines and only
/// the cells that hold money — the model reads a supplier's row, not a sea of zeros.</summary>
internal static partial class AiWeeklyCashflowGridTool
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string LaterWeek = "later";
    private const string CashInDirection = "in";
    private const string CashOutDirection = "out";

    private static object Shape(
        WeeklyCashflowView view,
        IReadOnlyList<WeeklyCashflowExportBand> bands,
        IReadOnlyList<WeeklyCashflowSeed> excluded,
        IReadOnlyList<WeeklyCashflowExclusion> exclusions,
        XeroAgedPayablesSnapshot payables,
        bool includeEntries) => new
    {
        ok = true,
        xeroFetchedAtUtc = payables.FetchedAtUtc,
        xeroTruncated = payables.Truncated,
        weekStarts = view.WeekStarts.Select(Day),
        laterIndex = view.LaterIndex,
        bands = bands.Select(band => new
        {
            band = band.Band.ToString(),
            label = band.Label,
            direction = band.IsCashIn ? CashInDirection : CashOutDirection,
            totalsByWeek = Cells(view, band.AmountIn, _ => false),
            total = band.Total,
            lines = band.Lines.Select(line => new
            {
                label = line.Label,
                entryCount = line.Entries.Count,
                total = line.Total,
                cells = Cells(view, line.AmountIn, line.HasMovedEntryIn),
                entries = includeEntries ? line.Entries.Select(entry => Entry(view, entry)) : null
            })
        }),
        netByWeek = view.Net,
        closingBalanceByWeek = view.Closing,
        lowestWeek = view.Closing is null ? null : Day(view.WeekStarts[view.MinClosingIndex]),
        excluded = excluded.Select(seed => new
        {
            seed.Label,
            seed.Detail,
            seed.Amount,
            excludedBy = exclusions.FirstOrDefault(exclusion => exclusion.PlacementKey == seed.PlacementKey)?.ExcludedByEmail
        }),
        note = "Cell index 0 is the current week and carries everything overdue; the last index is "
               + "Later (beyond the horizon). moved = the accountant placed money into that week on the "
               + "portal. Moving changes WHEN, never HOW MUCH; excluded entries are parked and uncounted."
    };

    // Only the cells that hold money, each named by its week's Monday.
    private static IEnumerable<object> Cells(WeeklyCashflowView view, Func<int, decimal> amountIn, Func<int, bool> movedIn)
    {
        for (var cellIndex = 0; cellIndex <= view.LaterIndex; cellIndex++)
        {
            var amount = amountIn(cellIndex);
            if (amount == 0m) continue;
            yield return new { weekIndex = cellIndex, weekStart = WeekLabel(view, cellIndex), amount, moved = movedIn(cellIndex) };
        }
    }

    private static object Entry(WeeklyCashflowView view, WeeklyCashflowEntry entry) => new
    {
        entry.Label,
        entry.Detail,
        entry.Amount,
        weekIndex = entry.WeekIndex,
        weekStart = WeekLabel(view, entry.WeekIndex),
        entry.Moved,
        dueOn = entry.NaturalDueOn is { } due ? Day(due) : null,
        expectedOn = entry.ExpectedOn is { } expected ? Day(expected) : null,
        entry.ItemId
    };

    private static string WeekLabel(WeeklyCashflowView view, int cellIndex) =>
        cellIndex == view.LaterIndex ? LaterWeek : Day(view.WeekStarts[cellIndex]);

    private static string Day(DateTimeOffset date) => date.UtcDateTime.ToString(DateFormat, CultureInfo.InvariantCulture);
}
