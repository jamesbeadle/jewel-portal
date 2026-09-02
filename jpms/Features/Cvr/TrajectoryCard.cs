namespace Jewel.JPMS.Features.Cvr;

/// <summary>One job's mini-chart on the trajectory panel: the running total of its months'
/// profit £ as a polyline on its own scale, the budgeted-profit band, and the headline
/// figures — built by the page from the same Xero months as the running grid.</summary>
public sealed record TrajectoryCard(
    Project Project,
    string PathPoints,
    double EndY,
    double? BudgetY,
    string LineColor,
    decimal PositionNow,
    decimal SixMonthDelta,
    decimal? Budget,
    bool Stale);
