namespace Jewel.JPMS.Features.Cvr;

// The cumulative-position cards' shape: one card per selected job, cumulative invoiced vs
// cumulative cost month by month from the job's first stored month to now, each on its own
// scale — built by the page from the stored Xero site P&L months, drawn by CumulativeChartCard.

public sealed record CumulativeMonthPoint(DateTime Month, decimal Invoiced, decimal Cost);

public sealed record CumulativeChart(
    IReadOnlyList<CumulativeMonthPoint> Points,
    string InvoicedPoints,
    string CostPoints,
    double InvoicedEndY,
    double CostEndY,
    decimal GrossProfit,
    decimal? Margin,
    string RangeLabel,
    string? Warning);

/// <summary>Chart null means "no line to draw" — Unavailable says why (unmapped vs no activity).</summary>
public sealed record CumulativeCard(Project Project, CumulativeChart? Chart, string? Unavailable);
