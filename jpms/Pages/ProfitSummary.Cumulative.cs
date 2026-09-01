using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Xero;

namespace Jewel.JPMS.Pages;

public partial class ProfitSummary
{
    // ---- Cumulative position (Xero site P&L) --------------------------------
    // One card per selected job: cumulative invoiced vs cumulative cost (cost of sales plus
    // overheads tracked to the site — the gap is then the accountant's operating profit for
    // the site, reconciling with his Xero P&L, 2026-08-12), month by month from the job's
    // first stored month to the current month, each card on its own scale. The figures are
    // Xero's own site P&L rows (XeroSitePnlReadModel — synced nightly, re-pulled by the
    // panel's Refresh button); months with no stored row are flat segments, so a stalled or
    // unbilled job reads as exactly that. Line colours are the accountant's mock pair,
    // validated for CVD separation and contrast on the card surface.

    private const string InvoicedColor = "#3987e5";
    private const string CostColor = "#d95926";

    private bool pnlFailed;
    private bool pnlSyncing;
    private string? pnlSyncError;
    // The sync's "finished cleanly but parked some projects for the next press" message —
    // amber, because it is progress to act on, not a failure to worry about.
    private string? pnlSyncNotice;

    private sealed record CumulativeMonthPoint(DateTime Month, decimal Invoiced, decimal Cost);

    private sealed record CumulativeChart(
        IReadOnlyList<CumulativeMonthPoint> Points,
        string InvoicedPoints,
        string CostPoints,
        double InvoicedEndY,
        double CostEndY,
        decimal GrossProfit,
        decimal? Margin,
        string RangeLabel,
        string? Warning);

    // Chart null means "no line to draw" — Unavailable says why (unmapped vs no activity).
    private sealed record CumulativeCard(Project Project, CumulativeChart? Chart, string? Unavailable);

    private List<CumulativeCard> CumulativeCardsFor(IReadOnlyList<Project> projects)
    {
        var rowsByProject = (EffectivePnlRows() ?? Array.Empty<XeroSiteMonthlyPnl>())
            .GroupBy(row => row.ProjectId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.OrderBy(row => row.Month).ToList(), StringComparer.OrdinalIgnoreCase);

        return projects
            .Select(project =>
                rowsByProject.TryGetValue(project.ProjectId, out var rows) && rows.Count > 0
                    ? new CumulativeCard(project, CumulativeChartFor(rows), null)
                    : new CumulativeCard(project, null,
                        string.IsNullOrWhiteSpace(project.XeroSiteName)
                            ? "Not mapped to a Xero site — set \"Xero site (tracking option)\" in the project's details, then refresh."
                            : "Nothing invoiced or spent against this site in Xero yet — the chart lights up once activity is tracked to it."))
            .ToList();
    }

    private static CumulativeChart CumulativeChartFor(IReadOnlyList<XeroSiteMonthlyPnl> rows)
    {
        // Every month from the job's first stored month to now, cumulated — absent months are
        // real flat segments, not gaps: "nothing moved" is part of the story.
        var first = new DateTime(rows[0].Month.Year, rows[0].Month.Month, 1);
        var last = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        if (last < first) last = first;

        var rowByMonth = rows.ToDictionary(row => new DateTime(row.Month.Year, row.Month.Month, 1));
        var points = new List<CumulativeMonthPoint>();
        decimal invoiced = 0m, cost = 0m;
        for (var month = first; month <= last; month = month.AddMonths(1))
        {
            if (rowByMonth.TryGetValue(month, out var row))
            {
                invoiced += row.Income;
                cost += row.CostOfSales + row.OperatingExpenses;
            }
            points.Add(new CumulativeMonthPoint(month, invoiced, cost));
        }

        // Each card on its own scale (the mock's point: one huge job must not squash the
        // rest flat), with 6% headroom so the top line never kisses the frame.
        var top = Math.Max(points.Max(point => point.Invoiced), points.Max(point => point.Cost));
        if (top <= 0m) top = 1m;
        top *= 1.06m;

        double XFor(int index) => points.Count == 1 ? 100d : index / (double)(points.Count - 1) * 100d;
        double YFor(decimal value) => (double)((top - value) / top) * 100d;
        string PointsAttr(Func<CumulativeMonthPoint, decimal> pick) =>
            string.Join(" ", points.Select((point, index) => $"{Pc(XFor(index))},{Pc(YFor(pick(point)))}"));

        var gross = invoiced - cost;
        decimal? margin = invoiced == 0m ? null : gross / invoiced;

        // The two flags from the accountant's mock. Underwater names the month the lines
        // crossed — the question a board asks next. Young deposit-led jobs carry absurd
        // margins until the work catches the money up, so the % is flagged, not hidden.
        string? warning = null;
        if (gross < 0m)
        {
            var crossed = points.FirstOrDefault(point => point.Cost > point.Invoiced);
            warning = crossed is null ? "cost ahead of invoicing" : $"cost overtook invoicing {crossed.Month:MMM yy}";
        }
        else if (points.Count <= 6 && margin is >= 0.3m)
        {
            warning = "deposit-led — margin not meaningful yet";
        }

        return new CumulativeChart(
            points,
            PointsAttr(point => point.Invoiced),
            PointsAttr(point => point.Cost),
            YFor(points[^1].Invoiced),
            YFor(points[^1].Cost),
            gross,
            margin,
            $"{first:MMM yy} → {last:MMM yy} · {points.Count} mo",
            warning);
    }

}
