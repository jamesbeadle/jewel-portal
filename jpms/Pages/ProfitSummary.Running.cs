using Jewel.JPMS.Commercial;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Cvr;
using Jewel.JPMS.Features.Xero;
using static Jewel.JPMS.Features.Cvr.ProfitDisplay;

namespace Jewel.JPMS.Pages;

public partial class ProfitSummary
{
    // ---- Running profit by month + trajectory (same Xero months as the cumulative panel) ----
    // Jeremy's running-balance format (2026-08-13): each cell's MAIN figure is the running %
    // to date at that month end — cumulative operating profit (income less cost of sales less
    // site-tracked overheads) over cumulative invoicing, the accountant's "Running % Profit"
    // row — so the row is the trend of the whole project and the last month equals "Position
    // now" by construction. The SMALL PRINT is the month's OWN margin (the month's profit over
    // the month's invoicing — his "Current Month % Profit") with the month's profit £ beside it
    // (2026-08-27, Jeremy: "I can't see the £ amount on profit"), and the cell is COLOURED on
    // the sign of that £ (2026-09-07 — until then the small print was the month's movement in
    // the running % and the colour followed it, so a profitable month on a high-margin job read
    // red because it pulled the average down). The movement in points lives in the hover. A
    // job with cost but no invoicing ever has no honest running % — the cell says n/a with the
    // £ beneath rather than printing an exploded figure; likewise a month invoicing under the
    // floor shows its £ only. The trajectory stays in £ — the running total the cumulative
    // panel's gap shows.

    // The low-invoicing floor (Jeremy, 2026-09-07): a month invoicing less than this shows its
    // £ only, greyed — the % would be noise. Page state with his default; the panel's own
    // control changes it for the session.
    private decimal monthPercentFloor = RunningMovement.DefaultMonthPercentFloor;

    // ---- The rolling window (Jeremy, 2026-09-10 and 2026-09-21) ----------------------------
    // "Go back further… see what the run rate was prior", then "read a job's run as a continuous
    // line, not in blocks": the grid shows the RunningWindowMonths months (6, 9 or 12, remembered
    // per user) ending at runningWindowEnd — this month on first open, one month earlier or later
    // per press, never past the first month any job on the grid has data for and never past this
    // month. A window reaching back before the grid's first month is trimmed to the months that
    // exist, so a four-month job in a twelve-month window is four columns, not eight blanks.
    // Moving the window moves ONLY the month columns: "N-mo Δ" is the running % now against N
    // months ago for the N months on screen, and "Position now" is the job to date, whichever
    // months are shown. The trajectory cards and the stale check keep their own fixed six months;
    // the Excel export carries whatever window is on screen.
    private DateTime runningWindowEnd = ThisMonth;

    private const int TrajectoryMonths = 6;
    private const int StaleMonths = 6;
    private static DateTime ThisMonth => new(DateTime.Today.Year, DateTime.Today.Month, 1);

    private static List<DateTime> WindowEndingAt(DateTime end, int length) =>
        Enumerable.Range(0, length).Select(offset => end.AddMonths(offset - (length - 1))).ToList();

    private List<DateTime> MonthsOnScreen(IReadOnlyList<Project> projects)
    {
        var window = WindowEndingAt(runningWindowEnd, RunningWindowMonths);
        if (EarliestGridMonth(projects) is not DateTime earliest) return window;
        var shown = window.Where(month => month >= earliest).ToList();
        return shown.Count == 0 ? window : shown;
    }

    // The earliest stored month across the jobs on the grid bounds the paging: a page that
    // could show nothing but "—" isn't offered.
    private DateTime? EarliestGridMonth(IReadOnlyList<Project> projects)
    {
        var all = EffectivePnlRows();
        if (all is null || all.Count == 0) return null;
        var ids = projects.Select(project => project.ProjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rows = all.Where(row => ids.Contains(row.ProjectId)).ToList();
        return rows.Count == 0 ? null : rows.Min(row => row.Month);
    }

    private bool IsLatestWindow => runningWindowEnd >= ThisMonth;
    private bool CanPageEarlier => EarliestGridMonth(GridProjects) is DateTime earliest
                                   && MonthsOnScreen(GridProjects)[0] > earliest;

    private void PageEarlier()
    {
        if (CanPageEarlier) runningWindowEnd = runningWindowEnd.AddMonths(-1);
    }

    private void PageLater()
    {
        var later = runningWindowEnd.AddMonths(1);
        runningWindowEnd = later > ThisMonth ? ThisMonth : later;
    }

    private void PageToNow() => runningWindowEnd = ThisMonth;

    // The site's operating profit — the accountant's definition (2026-08-12), so the grid
    // reconciles with his Xero P&L exactly.
    private static decimal ProfitOf(XeroSiteMonthlyPnl row) =>
        row.Income - row.CostOfSales - row.OperatingExpenses;

    /// <summary>The grid's model for the window on screen.</summary>
    private MovementModel? MovementFor(IReadOnlyList<Project> projects) =>
        MovementFor(projects, MonthsOnScreen(projects));

    /// <summary>The trajectory cards' model: always the latest six months, whatever the grid shows.</summary>
    private MovementModel? TrajectoryMovementFor(IReadOnlyList<Project> projects) =>
        MovementFor(projects, WindowEndingAt(ThisMonth, TrajectoryMonths));

    private MovementModel? MovementFor(IReadOnlyList<Project> projects, List<DateTime> months)
    {
        var all = EffectivePnlRows();
        if (all is null || all.Count == 0) return null;
        var byProject = all
            .GroupBy(row => row.ProjectId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        // The month columns on screen, and separately the same number of LATEST months, which
        // is what "N-mo Δ" compares across whichever months are shown; the stale check keeps
        // its own six.
        var latestMonths = WindowEndingAt(ThisMonth, months.Count);
        var staleMonths = WindowEndingAt(ThisMonth, StaleMonths);

        static MonthCell CellFor(List<XeroSiteMonthlyPnl> pnl, Func<XeroSiteMonthlyPnl, bool> inRange) =>
            new(pnl.Where(inRange).Sum(row => row.Income),
                pnl.Where(inRange).Sum(ProfitOf));

        static decimal? RunningBefore(List<XeroSiteMonthlyPnl> pnl, DateTime month)
        {
            var before = CellFor(pnl, row => row.Month < month);
            return before.Income == 0m ? (decimal?)null : before.Profit / before.Income * 100m;
        }

        // The running cells for one job (or the combined book): cumulative through each month
        // end, each carrying the prior month end's running % (the hover's movement and the ▲/▼
        // marker read off it) — seeded from the month BEFORE the window so the first column has
        // an honest prior too.
        List<RunningCell> RunningFor(List<XeroSiteMonthlyPnl> pnl, List<MonthCell> ownCells)
        {
            var cells = new List<RunningCell>();
            var previous = RunningBefore(pnl, months[0]);
            for (var index = 0; index < months.Count; index++)
            {
                var end = months[index].AddMonths(1);
                var toDate = CellFor(pnl, row => row.Month < end);
                var cell = new RunningCell(ownCells[index], toDate.Income, toDate.Profit, previous);
                cells.Add(cell);
                previous = cell.Running;
            }
            return cells;
        }

        // "N-mo Δ": the running % to date against the running % N months ago — the latest
        // window's baseline whatever months are on screen (null baseline — nothing invoiced
        // back then — means no honest Δ, the "—"). With the latest window on screen this is
        // exactly the last column against the month before the first.
        static decimal? WindowDelta(List<XeroSiteMonthlyPnl> pnl, DateTime latestStart, MonthCell toDate) =>
            toDate.Percent is decimal now && RunningBefore(pnl, latestStart) is decimal from ? now - from : (decimal?)null;

        List<MonthCell> OwnCellsFor(List<XeroSiteMonthlyPnl> pnl, List<DateTime> window) => window
            .Select(month => CellFor(pnl, row => row.Month.Year == month.Year && row.Month.Month == month.Month))
            .ToList();

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

            var runningCells = RunningFor(pnl, OwnCellsFor(pnl, months));
            // The to-date columns read the latest months, not the months on screen.
            var window = CellFor(pnl, row => row.Month >= latestMonths[0]);
            var running = CellFor(pnl, _ => true);
            // Stale needs history: a job whose data only starts inside the six months isn't
            // "stalled", it's young — only flag silence on jobs that were already running.
            var stale = OwnCellsFor(pnl, staleMonths).All(cell => Math.Abs(cell.Profit) < RunningMovement.ZeroThreshold
                                                                  && Math.Abs(cell.Income) < RunningMovement.ZeroThreshold)
                        && pnl.Any(row => row.Month < staleMonths[0]);

            rows.Add(new MovementRow(
                project,
                runningCells,
                window,
                WindowDelta(pnl, latestMonths[0], running),
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
        var columnTotals = RunningFor(combined, OwnCellsFor(combined, months));
        var totalWindow = CellFor(combined, row => row.Month >= latestMonths[0]);
        var totalRunning = CellFor(combined, _ => true);

        return new MovementModel(
            months, rows, columnTotals,
            totalWindow, WindowDelta(combined, latestMonths[0], totalRunning),
            totalRunning.Percent,
            rows.Sum(row => row.PositionMoney),
            excluded, monthPercentFloor);
    }

    private List<TrajectoryCard> TrajectoriesFor(MovementModel movement)
    {
        var all = EffectivePnlRows() ?? Array.Empty<XeroSiteMonthlyPnl>();
        var byProject = all
            .GroupBy(row => row.ProjectId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        // One more month-end point than the window has months: its intervals need a starting position.
        var points = Enumerable.Range(0, movement.Months.Count + 1).Select(offset => movement.Months[0].AddMonths(offset - 1)).ToList();

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
                row.MoneyWindowDelta,
                budget,
                row.Stale));
        }
        return cards;
    }

}
