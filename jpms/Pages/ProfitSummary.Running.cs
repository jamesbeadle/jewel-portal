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
    // ---- Running profit by month + trajectory (same Xero months as the cumulative panel) ----
    // Jeremy's running-balance format (2026-08-13): each cell's MAIN figure is the running %
    // to date at that month end — cumulative operating profit (income less cost of sales less
    // site-tracked overheads) over cumulative invoicing, the accountant's "Running % Profit"
    // row — so the row is the trend of the whole project and the last month equals "Position
    // now" by construction. The SMALL PRINT is the month's movement in percentage points
    // (main figure minus the prior month end's) with the month's own profit £ beside it
    // (2026-08-27, Jeremy: "I can't see the £ amount on profit"); the month's own % (the
    // 2026-08-12 "Current Month % Profit" format this replaced) lives in the hover, so a single
    // month's spike or fall is visible without being mistakable for the position. A job with cost but
    // no invoicing ever has no honest running % — the cell says n/a and carries the £ in its
    // hover rather than printing an exploded figure. The trajectory stays in £ — the running
    // total the cumulative panel's gap shows.

    // Money movements within £50 of zero read as no movement (stale detection, trajectory
    // "flat", and the grid's "—" for a month where nothing happened at all).
    private const decimal MovementZeroThreshold = 50m;

    // One month's (or window's) own figures. Percent is null when nothing was invoiced —
    // no base, no honest percentage (the n/a cell); Empty is a month where nothing moved
    // at all (the "—" cell).
    private sealed record MonthCell(decimal Income, decimal Profit)
    {
        public decimal? Percent => Income == 0m ? null : Profit / Income * 100m;
        public bool Empty => Income == 0m && Math.Abs(Profit) < MovementZeroThreshold;
    }

    // The grid's cell: the job to date at a month end. Running is the main figure (null while
    // nothing has ever been invoiced — no base, no honest percentage: the n/a cell); Empty is
    // a month end before anything had happened at all (the "—" cell). Own is that month's own
    // figures, kept for the hover.
    private sealed record RunningCell(MonthCell Own, decimal CumIncome, decimal CumProfit)
    {
        public decimal? Running => CumIncome == 0m ? null : CumProfit / CumIncome * 100m;
        public bool Empty => CumIncome == 0m && Math.Abs(CumProfit) < MovementZeroThreshold;
    }

    private sealed record MovementRow(
        Project Project,
        IReadOnlyList<RunningCell> MonthCells,  // the window's month ends, oldest first
        IReadOnlyList<decimal?> MovementsPp,    // per month: running % minus the prior month end's (null when either side has no %)
        MonthCell Window,                       // the six months' own figures taken together (the Δ cell's hover)
        decimal? WindowDelta,                   // running % now minus six months ago — "6-mo Δ" (null when there was no % back then)
        decimal? RunningPercent,                // running % to date — "Position now"
        decimal PositionMoney,                  // cumulative profit £ (the memo line)
        decimal MoneySixMonthDelta,             // £ over the window — the trajectory's headline
        bool Stale);

    private sealed record MovementModel(
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

    // The site's operating profit — the accountant's definition (2026-08-12), so the grid
    // reconciles with his Xero P&L exactly.
    private static decimal ProfitOf(XeroSiteMonthlyPnl row) =>
        row.Income - row.CostOfSales - row.OperatingExpenses;

    private MovementModel? MovementFor(IReadOnlyList<Project> projects)
    {
        var all = EffectivePnlRows();
        if (all is null || all.Count == 0) return null;
        var byProject = all
            .GroupBy(row => row.ProjectId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        // The last six months ending this month — the mock's window.
        var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var months = Enumerable.Range(0, 6).Select(offset => thisMonth.AddMonths(offset - 5)).ToList();

        static MonthCell CellFor(List<XeroSiteMonthlyPnl> pnl, Func<XeroSiteMonthlyPnl, bool> inRange) =>
            new(pnl.Where(inRange).Sum(row => row.Income),
                pnl.Where(inRange).Sum(ProfitOf));

        // The running cells and their movements for one job (or the combined book): cumulative
        // through each month end, each month's movement against the prior month end — seeded
        // from the month BEFORE the window so the first column has an honest movement too, and
        // the same seed is the 6-mo Δ's baseline (null baseline — nothing invoiced back then —
        // means no honest Δ, the "—").
        (List<RunningCell> Cells, List<decimal?> Moves, decimal? WindowDelta) RunningFor(List<XeroSiteMonthlyPnl> pnl, List<MonthCell> ownCells)
        {
            var beforeWindow = CellFor(pnl, row => row.Month < months[0]);
            var baseline = beforeWindow.Income == 0m ? (decimal?)null : beforeWindow.Profit / beforeWindow.Income * 100m;

            var cells = new List<RunningCell>();
            var moves = new List<decimal?>();
            var previous = baseline;
            for (var index = 0; index < months.Count; index++)
            {
                var end = months[index].AddMonths(1);
                var toDate = CellFor(pnl, row => row.Month < end);
                var cell = new RunningCell(ownCells[index], toDate.Income, toDate.Profit);
                cells.Add(cell);
                moves.Add(cell.Running is decimal current && previous is decimal prior ? current - prior : null);
                previous = cell.Running;
            }
            var windowDelta = cells[^1].Running is decimal last && baseline is decimal from ? last - from : (decimal?)null;
            return (cells, moves, windowDelta);
        }

        var rows = new List<MovementRow>();
        var excluded = new List<Project>();
        var included = new List<List<XeroSiteMonthlyPnl>>();
        foreach (var project in projects)
        {
            if (!byProject.TryGetValue(project.ProjectId, out var pnl) || pnl.Count == 0)
            {
                excluded.Add(project);
                continue;
            }
            included.Add(pnl);

            var ownCells = months
                .Select(month => CellFor(pnl, row => row.Month.Year == month.Year && row.Month.Month == month.Month))
                .ToList();
            var (runningCells, movements, windowDelta) = RunningFor(pnl, ownCells);
            var window = CellFor(pnl, row => row.Month >= months[0]);
            var running = CellFor(pnl, _ => true);
            // Stale needs history: a job whose data only starts inside the window isn't
            // "stalled", it's young — only flag silence on jobs that were already running.
            var stale = ownCells.All(cell => Math.Abs(cell.Profit) < MovementZeroThreshold
                                             && Math.Abs(cell.Income) < MovementZeroThreshold)
                        && pnl.Any(row => row.Month < months[0]);

            rows.Add(new MovementRow(
                project,
                runningCells,
                movements,
                window,
                windowDelta,
                running.Percent,
                running.Profit,
                window.Profit,
                stale));
        }
        if (rows.Count == 0) return null;

        // The portfolio row is the combined book's own running % (total profit to date over
        // total invoiced to date), NOT an average of the jobs' percentages — percentages
        // don't add across jobs.
        var combined = included.SelectMany(pnl => pnl).ToList();
        var combinedOwn = months
            .Select(month => CellFor(combined, row => row.Month.Year == month.Year && row.Month.Month == month.Month))
            .ToList();
        var (columnTotals, totalMovements, totalWindowDelta) = RunningFor(combined, combinedOwn);
        var totalWindow = CellFor(combined, row => row.Month >= months[0]);
        var totalRunning = CellFor(combined, _ => true);

        // Shade relative to the biggest movement, capped at 20pp so one wild early-job swing
        // (a first deposit landing on months of cost) can't wash every other cell out.
        var maxAbs = rows.SelectMany(row => row.MovementsPp)
            .Where(move => move is not null)
            .Select(move => Math.Abs(move!.Value))
            .DefaultIfEmpty(0m)
            .Max();
        var shadeMax = Math.Min(maxAbs, 20m);

        return new MovementModel(
            months, rows, columnTotals, totalMovements,
            totalWindow, totalWindowDelta,
            totalRunning.Percent,
            rows.Sum(row => row.PositionMoney),
            excluded, shadeMax);
    }

    /// <summary>The grid's cell shading: green improving, red worsening, alpha scaling with the movement's share of the biggest (capped).</summary>
    private static string MovementCellStyle(decimal movementPp, decimal shadeMax)
    {
        if (shadeMax <= 0m) return "background:#12151c";
        var alpha = (0.10m + 0.55m * Math.Min(Math.Abs(movementPp), shadeMax) / shadeMax)
            .ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        return movementPp < 0m
            ? $"background:rgba(194,85,85,{alpha})"
            : $"background:rgba(46,160,101,{alpha})";
    }

    /// <summary>The grid's unit: a percentage with one decimal ("26.5%", "−39.0%"), the accountant's rounding.</summary>
    private static string PctCell(decimal value) =>
        value >= 0m ? $"{value:0.0}%" : $"−{Math.Abs(value):0.0}%";

    /// <summary>A movement in percentage points, one decimal, always signed ("+15.3", "−11.8") — Jeremy's small print.</summary>
    private static string SignedPp(decimal value) =>
        value >= 0m ? $"+{value:0.0}" : $"−{Math.Abs(value):0.0}";

    /// <summary>The small-print line: the movement, or "—" for no meaningful movement (or no prior % to move from).</summary>
    private static string MovementPrint(decimal? movementPp) =>
        movementPp is decimal move && Math.Abs(move) >= 0.05m ? SignedPp(move) : "—";

    /// <summary>The running cell's hover: the position to date, then the month's own figures — the old grid's cell, preserved.</summary>
    private static string RunningCellHover(DateTime month, RunningCell cell)
    {
        var own = cell.Own.Percent is decimal ownPct
            ? $"this month: invoiced {MoneyCompact(cell.Own.Income)} · profit {SignedMoneyCompact(cell.Own.Profit)} ({PctCell(ownPct)})"
            : $"this month: nothing invoiced · profit {SignedMoneyCompact(cell.Own.Profit)}";
        return $"{month:MMM yy} — to date: invoiced {MoneyCompact(cell.CumIncome)} · profit {SignedMoneyCompact(cell.CumProfit)} · {own}";
    }

    /// <summary>Signed compact £ for the grid's hovers ("+£8.1k", "−£4.8k").</summary>
    private static string SignedMoneyCompact(decimal value) =>
        value >= 0m ? $"+{MoneyCompact(value)}" : MoneyCompact(value);

    /// <summary>Signed £k with one decimal — the trajectory's six-month headline ("+4.7", "−12.0").</summary>
    private static string DeltaK(decimal value) =>
        value >= 0m ? $"+{value / 1000m:0.0}" : $"−{Math.Abs(value) / 1000m:0.0}";

    private sealed record TrajectoryCard(
        Project Project,
        string PathPoints,
        double EndY,
        double? BudgetY,
        string LineColor,
        decimal PositionNow,
        decimal SixMonthDelta,
        decimal? Budget,
        bool Stale);

    private List<TrajectoryCard> TrajectoriesFor(MovementModel movement)
    {
        var all = EffectivePnlRows() ?? Array.Empty<XeroSiteMonthlyPnl>();
        var byProject = all
            .GroupBy(row => row.ProjectId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        // Seven month-end points: the window's six intervals need a starting position.
        var points = Enumerable.Range(0, 7).Select(offset => movement.Months[0].AddMonths(offset - 1)).ToList();

        var cards = new List<TrajectoryCard>();
        foreach (var row in movement.Rows)
        {
            var trajectoryPnl = byProject[row.Project.ProjectId];
            var values = points
                .Select(month => trajectoryPnl.Where(entry => entry.Month <= month).Sum(ProfitOf))
                .ToList();

            // Budget band from the table's own row — only when that project's reads have
            // landed; a band from unloaded data would be a guess, so it simply isn't drawn.
            decimal? budget = loadedProjects.Contains(row.Project.ProjectId)
                ? RowFor(row.Project.ProjectId).BudgetedProfit
                : null;

            // Each card on its own scale, band included so it is always visible (the mock's
            // 12% padding keeps the line off the frame).
            var low = values.Min();
            var high = values.Max();
            if (budget is decimal b) { low = Math.Min(low, b); high = Math.Max(high, b); }
            var pad = (high - low) * 0.12m;
            if (pad == 0m) pad = 1m;
            low -= pad;
            high += pad;

            double YFor(decimal value) => (double)((high - value) / (high - low)) * 100d;
            double XFor(int index) => index / (double)(points.Count - 1) * 100d;
            var path = string.Join(" ", values.Select((value, index) => $"{Pc(XFor(index))},{Pc(YFor(value))}"));

            // The mock's rule: red when the job sits under its budget band, green when on
            // or above it; without a band, the position's own sign colours the line.
            var underBudget = budget is decimal target ? values[^1] < target : values[^1] < 0m;

            cards.Add(new TrajectoryCard(
                row.Project,
                path,
                YFor(values[^1]),
                budget is decimal band ? YFor(band) : null,
                underBudget ? "#c25555" : "#2ea065",
                row.PositionMoney,
                row.MoneySixMonthDelta,
                budget,
                row.Stale));
        }
        return cards;
    }

    private async Task LoadSitePnlAsync()
    {
        try
        {
            await SitePnl.RefreshAsync(CancellationToken.None);
            pnlFailed = false;
        }
        catch
        {
            // HttpQueryClient has already reported this to the error toast with a reference;
            // the flag opens the panel's gate so it says what went wrong instead of pulsing.
            pnlFailed = true;
        }
    }

    // The panel's Refresh button: re-pull from Xero (SyncXeroSitePnl reads the P&L report per
    // mapped project), then re-read the stored rows. A sync that completes but reports an
    // error (e.g. a renamed tracking option) is shown inline — it is an answer, not a toast.
    private async Task RefreshSitePnlAsync()
    {
        pnlSyncing = true;
        pnlSyncError = null;
        pnlSyncNotice = null;
        try
        {
            var result = await Commands.SendAsync(new SyncXeroSitePnl(), CancellationToken.None);
            if (result.Error is not null) pnlSyncError = result.Error;
            pnlSyncNotice = result.Notice;
            await LoadSitePnlAsync();
        }
        catch
        {
            // Command/query failures are already on the error toast with a reference.
        }
        finally
        {
            pnlSyncing = false;
        }
    }

}
