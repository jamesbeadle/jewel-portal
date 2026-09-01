namespace Jewel.JPMS.Features.Cvr;

// Jeremy's running-balance format (2026-08-13): each cell's MAIN figure is the running % to
// date at that month end; the SMALL PRINT is the month's movement in percentage points with the
// month's own profit £ beside it. The records here are that grid's shape — built by the Profit
// Summary page, rendered by RunningProfitTable.

/// <summary>Money movements within £50 of zero read as no movement (stale detection, trajectory
/// "flat", and the grid's "—" for a month where nothing happened at all).</summary>
public static class RunningMovement
{
    public const decimal ZeroThreshold = 50m;
}

/// <summary>One month's (or window's) own figures. Percent is null when nothing was invoiced —
/// no base, no honest percentage (the n/a cell); Empty is a month where nothing moved at all
/// (the "—" cell).</summary>
public sealed record MonthCell(decimal Income, decimal Profit)
{
    public decimal? Percent => Income == 0m ? null : Profit / Income * 100m;
    public bool Empty => Income == 0m && Math.Abs(Profit) < RunningMovement.ZeroThreshold;
}

/// <summary>The grid's cell: the job to date at a month end. Running is the main figure (null
/// while nothing has ever been invoiced — no base, no honest percentage: the n/a cell); Empty is
/// a month end before anything had happened at all (the "—" cell). Own is that month's own
/// figures, kept for the hover.</summary>
public sealed record RunningCell(MonthCell Own, decimal CumIncome, decimal CumProfit)
{
    public decimal? Running => CumIncome == 0m ? null : CumProfit / CumIncome * 100m;
    public bool Empty => CumIncome == 0m && Math.Abs(CumProfit) < RunningMovement.ZeroThreshold;
}

public sealed record MovementRow(
    Project Project,
    IReadOnlyList<RunningCell> MonthCells,  // the window's month ends, oldest first
    IReadOnlyList<decimal?> MovementsPp,    // per month: running % minus the prior month end's (null when either side has no %)
    MonthCell Window,                       // the six months' own figures taken together (the Δ cell's hover)
    decimal? WindowDelta,                   // running % now minus six months ago — "6-mo Δ" (null when there was no % back then)
    decimal? RunningPercent,                // running % to date — "Position now"
    decimal PositionMoney,                  // cumulative profit £ (the memo line)
    decimal MoneySixMonthDelta,             // £ over the window — the trajectory's headline
    bool Stale);

public sealed record MovementModel(
    IReadOnlyList<DateTime> Months,
    IReadOnlyList<MovementRow> Rows,
    IReadOnlyList<RunningCell> ColumnTotals,   // the combined book to date, per month end
    IReadOnlyList<decimal?> TotalMovementsPp,
    MonthCell TotalWindow,
    decimal? TotalWindowDelta,
    decimal? TotalRunningPercent,
    decimal TotalPositionMoney,
    IReadOnlyList<Project> Excluded,
    decimal ShadeMax);
