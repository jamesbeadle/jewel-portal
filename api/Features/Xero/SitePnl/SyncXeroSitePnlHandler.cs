using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Xero.SitePnl;

/// <summary>
/// Pulls mapped projects' site P&amp;L from Xero — the profit &amp; loss report filtered by
/// the project's "Sites" tracking option, monthly columns — and upserts the figures into
/// XeroSitePnlMonths. Every field is Xero-owned, so a sync refreshes whatever window it
/// reads: months inside the window that come back all-zero (a recode in Xero — lines moved
/// off the site) are removed rather than left as stale rows; months outside the window are
/// left alone.
///
/// Two modes (see the command's remarks). Interactive (FullHistory false): the last twelve
/// months per project — one Xero call each — least-recently-synced projects first, under a
/// soft time budget so the HTTP request finishes inside the Static Web Apps gateway's ~45s
/// limit; projects that don't fit are parked for the next press (the Notice says so).
/// Nightly (FullHistory true): the full window from XeroOptions.FromDate, no time budget.
/// Either mode gives a project with no stored rows its full backfill. Projects with no
/// Xero site mapping are skipped and named in the result; leads are skipped silently —
/// they have no site yet, so their absence is not a problem to report. A Xero failure
/// (rate limit despite the client's retries, or a renamed tracking option) stops the run
/// but keeps the projects that completed — each project's months are independently true —
/// and the error names the project it stopped at.
/// </summary>
public sealed class SyncXeroSitePnlHandler : ICommandHandler<SyncXeroSitePnl, XeroSitePnlSyncResult>
{
    // Interactive runs park remaining projects past this point — comfortably inside the
    // SWA gateway's ~45s, leaving room for one 429 retry wait on the project in flight.
    private static readonly TimeSpan SoftTimeBudget = TimeSpan.FromSeconds(25);

    private readonly IXeroClient xero;
    private readonly JpmsContext context;
    private readonly XeroOptions options;

    public SyncXeroSitePnlHandler(IXeroClient xero, JpmsContext context, XeroOptions options)
    {
        this.xero = xero;
        this.context = context;
        this.options = options;
    }

    public async Task<XeroSitePnlSyncResult> HandleAsync(SyncXeroSitePnl command, CancellationToken cancellationToken)
    {
        if (!xero.IsConfigured)
            return new XeroSitePnlSyncResult(false, null, 0, 0, Array.Empty<string>());

        var projects = await context.Projects.ToListAsync(cancellationToken);
        var mapped = projects
            .Where(project => !string.IsNullOrWhiteSpace(project.XeroSiteName))
            .ToList();
        var unmappedNames = projects
            .Where(project => string.IsNullOrWhiteSpace(project.XeroSiteName)
                              && project.Stage != (int)ProjectStage.Lead)
            .Select(project => project.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var today = DateTime.UtcNow.Date;
        var windowStart = new DateTime(options.FromDate.Year, options.FromDate.Month, 1);
        var toMonth = new DateTime(today.Year, today.Month, 1);
        // The interactive window: the current month plus the eleven before it — one Xero
        // call per project, because a job's older months don't change (and when they do,
        // the nightly full-history run catches it within a day).
        var recentStart = toMonth.AddMonths(-11) < windowStart ? windowStart : toMonth.AddMonths(-11);

        var storedByProject = (await context.XeroSitePnlMonths.ToListAsync(cancellationToken))
            .GroupBy(row => row.ProjectId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        // Least-recently-synced first (never-synced counts as oldest): a run that stops on
        // the time budget makes progress every press instead of redoing the same projects.
        var ordered = mapped
            .OrderBy(project => storedByProject.TryGetValue(project.ProjectId, out var rows) && rows.Count > 0
                ? rows.Max(row => row.LastSyncedAtUtc)
                : DateTimeOffset.MinValue)
            .ToList();

        var now = DateTimeOffset.UtcNow;
        var monthsStored = 0;
        var projectsSynced = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        foreach (var project in ordered)
        {
            // Soft budget, interactive runs only: park the rest for the next press rather
            // than letting the SWA gateway kill the request at ~45s and report a 500 for a
            // sync that was actually working. At least one project always runs.
            if (!command.FullHistory && projectsSynced > 0 && stopwatch.Elapsed > SoftTimeBudget)
            {
                await context.SaveChangesAsync(cancellationToken);
                return new XeroSitePnlSyncResult(
                    true, null, projectsSynced, monthsStored, unmappedNames,
                    $"Synced {projectsSynced} of {ordered.Count} projects before the time budget ran out — press Refresh again to continue.");
            }

            var hasStoredRows = storedByProject.TryGetValue(project.ProjectId, out var storedRows) && storedRows.Count > 0;
            var fromMonth = command.FullHistory || !hasStoredRows ? windowStart : recentStart;

            IReadOnlyList<XeroSitePnlMonthFigures> figures;
            try
            {
                figures = await xero.GetSiteMonthlyPnlAsync(project.XeroSiteName!, fromMonth, toMonth, cancellationToken);
            }
            catch (XeroCallFailedException failure)
            {
                // Xero refused mid-run — usually the rate limit, occasionally a renamed
                // tracking option. Each project's months are independently true, so keep what
                // completed and report which project stopped the run: the next sync (button
                // or nightly) finishes the rest from Xero's current state.
                await context.SaveChangesAsync(cancellationToken);
                return new XeroSitePnlSyncResult(
                    true, $"{project.Name}: {failure.Message}", projectsSynced, monthsStored, unmappedNames);
            }

            var stored = storedByProject.TryGetValue(project.ProjectId, out var rows)
                ? rows
                : new List<XeroSitePnlMonthEntity>();
            var storedById = stored.ToDictionary(row => row.XeroSitePnlMonthId, StringComparer.OrdinalIgnoreCase);
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var month in figures)
            {
                var id = $"{project.ProjectId}:{month.Month:yyyy-MM}";
                seenIds.Add(id);

                if (!storedById.TryGetValue(id, out var entity))
                {
                    entity = new XeroSitePnlMonthEntity
                    {
                        XeroSitePnlMonthId = id,
                        ProjectId = project.ProjectId,
                        Month = month.Month
                    };
                    context.XeroSitePnlMonths.Add(entity);
                }

                entity.Income = month.Income;
                entity.CostOfSales = month.CostOfSales;
                entity.OperatingExpenses = month.OperatingExpenses;
                entity.LastSyncedAtUtc = now;
                monthsStored++;
            }

            // Stored months inside the synced window that Xero no longer reports any
            // movement for — a recode reversed them; keep months outside the window
            // (a FromDate moved forward must not silently delete history).
            foreach (var entity in stored)
            {
                if (entity.Month >= fromMonth && entity.Month <= toMonth && !seenIds.Contains(entity.XeroSitePnlMonthId))
                    context.XeroSitePnlMonths.Remove(entity);
            }

            projectsSynced++;
        }

        await context.SaveChangesAsync(cancellationToken);

        return new XeroSitePnlSyncResult(true, null, projectsSynced, monthsStored, unmappedNames);
    }
}
