using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Xero.SitePnl;

/// <summary>
/// Pulls every mapped project's site P&amp;L from Xero — the profit &amp; loss report
/// filtered by the project's "Sites" tracking option, monthly columns from the reporting
/// window's start (XeroOptions.FromDate) to the current month — and upserts the figures
/// into XeroSitePnlMonths. Every field is Xero-owned, so a sync is a full refresh: months
/// that come back all-zero (a recode in Xero — lines moved off the site) are removed
/// rather than left as stale rows, and a missed night self-heals the same way the ledger
/// sync does. Projects with no Xero site mapping are skipped and named in the result;
/// leads are skipped silently — they have no site yet, so their absence is not a problem
/// to report. A Xero failure stops the run and reports the error; whatever saved before
/// the failure would have saved again next run, so nothing is left half-true.
/// </summary>
public sealed class SyncXeroSitePnlHandler : ICommandHandler<SyncXeroSitePnl, XeroSitePnlSyncResult>
{
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
        var fromMonth = new DateTime(options.FromDate.Year, options.FromDate.Month, 1);
        var toMonth = new DateTime(today.Year, today.Month, 1);

        var storedByProject = (await context.XeroSitePnlMonths.ToListAsync(cancellationToken))
            .GroupBy(row => row.ProjectId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var now = DateTimeOffset.UtcNow;
        var monthsStored = 0;
        var projectsSynced = 0;

        foreach (var project in mapped)
        {
            IReadOnlyList<XeroSitePnlMonthFigures> figures;
            try
            {
                figures = await xero.GetSiteMonthlyPnlAsync(project.XeroSiteName!, fromMonth, toMonth, cancellationToken);
            }
            catch (XeroCallFailedException failure)
            {
                // Nothing is saved on a failed run: a partial refresh that stamped some
                // projects and not others would read as "synced" while lying about half
                // the chart. The error names the project so the mapping fault is findable.
                return new XeroSitePnlSyncResult(
                    true, $"{project.Name}: {failure.Message}", projectsSynced, 0, unmappedNames);
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
