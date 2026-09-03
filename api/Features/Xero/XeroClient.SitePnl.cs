using System.Globalization;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    // 2026-09-03 (the accountant's "entries dated the 31st fall out of their month"): Xero
    // derives a multi-period P&L's comparison columns from the BASE period's dates. A base
    // period ending on the 30th (the current month was September) made every earlier column
    // end on the 30th too, so the 31st of every 31-day month — salary journals, month-end
    // bills — vanished from the grid for the whole of a job's history. Two defences now:
    //   1. every window's base period is a 31-day month (the window-end month, or the month
    //      after it when that has 30 days or fewer — two short months never run together), so
    //      each comparison column's end date clamps to its own true month-end;
    //   2. the report's header dates are checked column by column against the month-end each
    //      column is expected to cover, and any month Xero still returned short is re-read
    //      alone as a plain 1st-to-last-day range — the shape the accountant reconciles against.
    public async Task<IReadOnlyList<XeroSitePnlMonthFigures>> GetSiteMonthlyPnlAsync(
        string siteOption, DateTime fromMonth, DateTime toMonth, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return Array.Empty<XeroSitePnlMonthFigures>();

        var token = await GetAccessTokenAsync(ct);
        var (categoryId, optionId) = await ResolveSiteOptionAsync(token, siteOption, ct);

        var first = new DateTime(fromMonth.Year, fromMonth.Month, 1);
        var last = new DateTime(toMonth.Year, toMonth.Month, 1);
        if (last < first) return Array.Empty<XeroSitePnlMonthFigures>();

        var byMonth = new SortedDictionary<DateTime, PnlTotals>();
        var shortMonths = new List<DateTime>();

        // Newest window first, stepping back: each call covers the base month plus up to
        // eleven earlier monthly columns (Xero's periods cap). The base is always a 31-day
        // month, which can sit one month past the window's end — that column is then simply
        // outside the requested range and ignored.
        var windowEnd = last;
        while (windowEnd >= first)
        {
            var baseMonth = ThirtyOneDayBaseFor(windowEnd);
            var windowStart = baseMonth.AddMonths(-11) < first ? first : baseMonth.AddMonths(-11);
            var periods = MonthsBetween(windowStart, baseMonth);
            var url = $"{ProfitAndLossReportUrl}?fromDate={baseMonth:yyyy-MM-dd}"
                      + $"&toDate={LastDayOf(baseMonth):yyyy-MM-dd}"
                      + $"&trackingCategoryID={categoryId}&trackingOptionID={optionId}"
                      + (periods > 0 ? $"&timeframe=MONTH&periods={periods}" : "");

            using (var doc = await GetPnlReportAsync(token, url, ct))
            {
                var columns = ReadPnlColumns(doc);
                var headerEnds = ReadHeaderEndDates(doc);
                for (var column = 0; column <= periods && column < columns.Count; column++)
                {
                    var month = baseMonth.AddMonths(-column);
                    if (month < first || month > last) continue;

                    if (headerEnds is not null && column < headerEnds.Count
                        && headerEnds[column] is { } headerEnd
                        && headerEnd.Date != LastDayOf(month))
                    {
                        // Xero cut this column short (or shifted it): re-read the month alone.
                        shortMonths.Add(month);
                        continue;
                    }
                    byMonth[month] = columns[column];
                }
            }

            windowEnd = windowStart.AddMonths(-1);
        }

        foreach (var month in shortMonths)
        {
            _logger.LogInformation(
                "Xero P&L column for {Site} {Month:yyyy-MM} did not end on the month-end; re-reading the month alone.",
                siteOption, month);
            byMonth[month] = await GetSiteRangePnlAsync(token, categoryId, optionId, month, LastDayOf(month), ct);
        }

        return byMonth
            .Where(entry => entry.Value.Income != 0m
                            || entry.Value.CostOfSales != 0m
                            || entry.Value.OperatingExpenses != 0m)
            .Select(entry => new XeroSitePnlMonthFigures(
                entry.Key, entry.Value.Income, entry.Value.CostOfSales, entry.Value.OperatingExpenses))
            .ToList();
    }

    public async Task<XeroSitePnlRangeFigures?> GetSiteRangePnlAsync(
        string siteOption, DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        if (!_options.IsConfigured || toDate.Date < fromDate.Date) return null;

        var token = await GetAccessTokenAsync(ct);
        var (categoryId, optionId) = await ResolveSiteOptionAsync(token, siteOption, ct);
        var totals = await GetSiteRangePnlAsync(token, categoryId, optionId, fromDate.Date, toDate.Date, ct);
        return new XeroSitePnlRangeFigures(
            fromDate.Date, toDate.Date, totals.Income, totals.CostOfSales, totals.OperatingExpenses);
    }

    /// <summary>One plain date range, no comparison periods — a single column.</summary>
    private async Task<PnlTotals> GetSiteRangePnlAsync(
        string token, string categoryId, string optionId, DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var url = $"{ProfitAndLossReportUrl}?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}"
                  + $"&trackingCategoryID={categoryId}&trackingOptionID={optionId}";
        using var doc = await GetPnlReportAsync(token, url, ct);
        var columns = ReadPnlColumns(doc);
        return columns.Count > 0 ? columns[0] : default;
    }

    private async Task<(string CategoryId, string OptionId)> ResolveSiteOptionAsync(
        string token, string siteOption, CancellationToken ct)
    {
        var categories = await GetTrackingCategoriesAsync(token, ct);
        if (!categories.SiteOptionIdsByName.TryGetValue(siteOption, out var optionId))
            throw new XeroCallFailedException(
                $"Xero's \"{_options.SiteTrackingCategory}\" tracking category has no option named "
                + $"\"{siteOption}\" — check the project's Xero site mapping against Xero's tracking options.");
        return (categories.SiteCategoryId, optionId);
    }

    private async Task<JsonDocument> GetPnlReportAsync(string token, string url, CancellationToken ct)
    {
        try
        {
            return await GetJsonAsync(token, url, "site profit and loss report", ct);
        }
        catch (XeroCallFailedException failure) when (failure.Message.Contains("HTTP 403"))
        {
            throw new XeroCallFailedException(
                "Couldn't read Xero's profit and loss report — the Xero custom connection needs the "
                + "accounting.reports.read scope ticked in the Xero developer portal. " + failure.Message);
        }
    }

    /// <summary>The month itself when it has 31 days, otherwise the month after (which always does).</summary>
    private static DateTime ThirtyOneDayBaseFor(DateTime month)
    {
        var start = new DateTime(month.Year, month.Month, 1);
        return DateTime.DaysInMonth(start.Year, start.Month) == 31 ? start : start.AddMonths(1);
    }

    private static DateTime LastDayOf(DateTime month) =>
        new(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));

    private static int MonthsBetween(DateTime earlier, DateTime later) =>
        ((later.Year - earlier.Year) * 12) + later.Month - earlier.Month;

    private readonly record struct PnlTotals(decimal Income, decimal CostOfSales, decimal OperatingExpenses);

    private enum PnlBucket { Income, CostOfSales, OperatingExpenses }

    // Header cell shapes Xero has shipped for period columns; the day is what matters here.
    private static readonly string[] HeaderDateFormats =
    {
        "d MMM yy", "d MMM yyyy", "dd MMM yy", "dd MMM yyyy", "d MMMM yyyy", "dd MMMM yyyy",
        "yyyy-MM-dd", "d/M/yyyy", "dd/MM/yyyy",
    };

    /// <summary>
    /// The period-end date of each amount column, base period first, read from the report's
    /// header row. Null when the header is missing or none of its cells are dates — the caller
    /// then trusts the 31-day anchoring rather than re-reading every month.
    /// </summary>
    private List<DateTime?>? ReadHeaderEndDates(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("Reports", out var reports)
            || reports.ValueKind != JsonValueKind.Array || reports.GetArrayLength() == 0)
            return null;

        foreach (var row in RowsOf(reports[0]))
        {
            if (StringOf(row, "RowType") != "Header") continue;
            if (!row.TryGetProperty("Cells", out var cells) || cells.ValueKind != JsonValueKind.Array)
                return null;

            var ends = new List<DateTime?>();
            var anyParsed = false;
            for (var index = 1; index < cells.GetArrayLength(); index++)
            {
                var text = StringOf(cells[index], "Value")?.Trim();
                if (!string.IsNullOrEmpty(text)
                    && (DateTime.TryParseExact(text, HeaderDateFormats, CultureInfo.InvariantCulture,
                            DateTimeStyles.AllowWhiteSpaces, out var parsed)
                        || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsed)))
                {
                    ends.Add(parsed.Date);
                    anyParsed = true;
                }
                else
                {
                    ends.Add(null);
                }
            }
            if (!anyParsed && cells.GetArrayLength() > 1)
                _logger.LogWarning(
                    "Xero P&L header cells aren't parseable dates ({Sample}); column month-ends can't be verified.",
                    StringOf(cells[1], "Value"));
            return anyParsed ? ends : null;
        }
        return null;
    }

    /// <summary>
    /// One report's amount columns as totals, base period first (with comparison periods the
    /// columns run newest → oldest — the documented order — so the caller maps column N to the
    /// base month minus N). Section totals are classified by title or summary label ("Total
    /// Income", "Total Cost of Sales", "Total Operating Expenses" — tolerant of Xero's wording
    /// variants); the Gross/Net Profit sections are derived figures and deliberately skipped.
    /// Sections without a summary row fall back to summing their detail rows.
    /// </summary>
    private static List<PnlTotals> ReadPnlColumns(JsonDocument doc)
    {
        var totals = new List<PnlTotals>();
        if (!doc.RootElement.TryGetProperty("Reports", out var reports)
            || reports.ValueKind != JsonValueKind.Array || reports.GetArrayLength() == 0)
            return totals;

        foreach (var section in RowsOf(reports[0]))
        {
            if (StringOf(section, "RowType") != "Section") continue;

            // The section's own title decides the bucket; failing that, the summary row's
            // label ("Total Income") — Xero has shipped both shapes.
            var bucket = PnlBucketOf(StringOf(section, "Title"));

            JsonElement? summaryCells = null;
            var detailTotals = new Dictionary<int, decimal>();
            foreach (var row in RowsOf(section))
            {
                var rowType = StringOf(row, "RowType");
                if (!row.TryGetProperty("Cells", out var cells)
                    || cells.ValueKind != JsonValueKind.Array || cells.GetArrayLength() < 2)
                    continue;

                if (rowType == "SummaryRow")
                {
                    summaryCells = cells;
                    bucket ??= PnlBucketOf(StringOf(cells[0], "Value"));
                }
                else if (rowType == "Row")
                {
                    for (var index = 1; index < cells.GetArrayLength(); index++)
                        detailTotals[index] = (detailTotals.TryGetValue(index, out var sum) ? sum : 0m)
                                              + CellDecimal(cells[index]);
                }
            }
            if (bucket is null) continue;

            decimal ValueAt(int cellIndex) => summaryCells is { } summary
                ? cellIndex < summary.GetArrayLength() ? CellDecimal(summary[cellIndex]) : 0m
                : detailTotals.TryGetValue(cellIndex, out var sum) ? sum : 0m;

            var columns = summaryCells is { } summaryRow
                ? summaryRow.GetArrayLength() - 1
                : detailTotals.Count == 0 ? 0 : detailTotals.Keys.Max();
            while (totals.Count < columns) totals.Add(default);
            for (var column = 0; column < columns; column++)
            {
                var value = ValueAt(column + 1); // first cell is the label
                var current = totals[column];
                totals[column] = bucket switch
                {
                    PnlBucket.Income => current with { Income = current.Income + value },
                    PnlBucket.CostOfSales => current with { CostOfSales = current.CostOfSales + value },
                    _ => current with { OperatingExpenses = current.OperatingExpenses + value },
                };
            }
        }
        return totals;
    }

    /// <summary>
    /// Which P&L bucket a section belongs to, from its title or summary label. Cost of sales
    /// is tested before income because "Total Cost of Sales" contains "sales"; the derived
    /// profit sections return null so they are never double-counted.
    /// </summary>
    private static PnlBucket? PnlBucketOf(string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return null;
        var normalised = Normalise(label);
        if (normalised.Contains("grossprofit") || normalised.Contains("netprofit")
            || normalised.Contains("grossloss") || normalised.Contains("netloss"))
            return null;
        if (normalised.Contains("costofsales") || normalised.Contains("directcost"))
            return PnlBucket.CostOfSales;
        if (normalised.Contains("operatingexpense") || normalised.Contains("overhead")
            || normalised is "expenses" or "totalexpenses" or "lessexpenses")
            return PnlBucket.OperatingExpenses;
        if (normalised.Contains("income") || normalised.Contains("turnover")
            || normalised.Contains("revenue") || normalised.Contains("sales"))
            return PnlBucket.Income;
        return null;
    }
}
