using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    public async Task<IReadOnlyList<XeroSitePnlMonthFigures>> GetSiteMonthlyPnlAsync(
        string siteOption, DateTime fromMonth, DateTime toMonth, CancellationToken ct)
    {
        if (!_options.IsConfigured)
            return Array.Empty<XeroSitePnlMonthFigures>();

        var token = await GetAccessTokenAsync(ct);
        var categories = await GetTrackingCategoriesAsync(token, ct);
        if (!categories.SiteOptionIdsByName.TryGetValue(siteOption, out var optionId))
            throw new XeroCallFailedException(
                $"Xero's \"{_options.SiteTrackingCategory}\" tracking category has no option named "
                + $"\"{siteOption}\" — check the project's Xero site mapping against Xero's tracking options.");

        var first = new DateTime(fromMonth.Year, fromMonth.Month, 1);
        var last = new DateTime(toMonth.Year, toMonth.Month, 1);
        if (last < first) return Array.Empty<XeroSitePnlMonthFigures>();

        // Newest window first, stepping back twelve months per call: each call covers the
        // window-end month plus up to eleven earlier monthly columns (Xero's periods cap).
        var byMonth = new SortedDictionary<DateTime, PnlTotals>();
        for (var windowEnd = last; windowEnd >= first; windowEnd = windowEnd.AddMonths(-12))
        {
            var windowStart = windowEnd.AddMonths(-11) < first ? first : windowEnd.AddMonths(-11);
            var periods = ((windowEnd.Year - windowStart.Year) * 12) + windowEnd.Month - windowStart.Month;
            var url = $"{ProfitAndLossReportUrl}?fromDate={windowEnd:yyyy-MM-dd}"
                      + $"&toDate={windowEnd.AddMonths(1).AddDays(-1):yyyy-MM-dd}"
                      + $"&trackingCategoryID={categories.SiteCategoryId}&trackingOptionID={optionId}"
                      + (periods > 0 ? $"&timeframe=MONTH&periods={periods}" : "");

            JsonDocument doc;
            try
            {
                doc = await GetJsonAsync(token, url, "site profit and loss report", ct);
            }
            catch (XeroCallFailedException failure) when (failure.Message.Contains("HTTP 403"))
            {
                throw new XeroCallFailedException(
                    "Couldn't read Xero's profit and loss report — the Xero custom connection needs the "
                    + "accounting.reports.read scope ticked in the Xero developer portal. " + failure.Message);
            }

            using (doc)
            {
                ReadPnlColumns(doc, windowEnd, periods, byMonth);
            }
        }

        return byMonth
            .Where(entry => entry.Value.Income != 0m
                            || entry.Value.CostOfSales != 0m
                            || entry.Value.OperatingExpenses != 0m)
            .Select(entry => new XeroSitePnlMonthFigures(
                entry.Key, entry.Value.Income, entry.Value.CostOfSales, entry.Value.OperatingExpenses))
            .ToList();
    }

    private readonly record struct PnlTotals(decimal Income, decimal CostOfSales, decimal OperatingExpenses);

    private enum PnlBucket { Income, CostOfSales, OperatingExpenses }

    /// <summary>
    /// Reads one report's monthly columns into <paramref name="into"/>. With comparison
    /// periods the amount columns run newest → oldest, base period first — months are derived
    /// from that documented order rather than parsed out of the header labels, whose date
    /// format has varied. A column that doesn't align with a requested month is ignored.
    /// Section totals are classified by title or summary label ("Total Income", "Total Cost
    /// of Sales", "Total Operating Expenses" — tolerant of Xero's wording variants); the
    /// Gross/Net Profit sections are derived figures and deliberately skipped. Sections
    /// without a summary row fall back to summing their detail rows.
    /// </summary>
    private static void ReadPnlColumns(
        JsonDocument doc, DateTime windowEnd, int periods, SortedDictionary<DateTime, PnlTotals> into)
    {
        if (!doc.RootElement.TryGetProperty("Reports", out var reports)
            || reports.ValueKind != JsonValueKind.Array || reports.GetArrayLength() == 0)
            return;

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
            for (var column = 0; column < columns && column <= periods; column++)
            {
                var month = windowEnd.AddMonths(-column);
                var value = ValueAt(column + 1); // first cell is the label
                var current = into.TryGetValue(month, out var existing) ? existing : default;
                into[month] = bucket switch
                {
                    PnlBucket.Income => current with { Income = current.Income + value },
                    PnlBucket.CostOfSales => current with { CostOfSales = current.CostOfSales + value },
                    _ => current with { OperatingExpenses = current.OperatingExpenses + value },
                };
            }
        }
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

    // -- attachments: the supplier's document, viewed from the allocation page ---------

}
