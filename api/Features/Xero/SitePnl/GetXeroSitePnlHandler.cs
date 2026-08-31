using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Xero.SitePnl;

/// <summary>
/// The stored site P&amp;L, straight from XeroSitePnlMonths — a database read, never a live
/// Xero call (the nightly worker and the explicit sync command own the refresh). Ordered per
/// project oldest month first, which is the order the cumulative chart accumulates in.
/// Alongside the stored Xero rows the snapshot carries the labour accrual read
/// (<see cref="XeroSiteMonthlyLabourAccrual"/>): approved timesheet cost not yet settled by a
/// bill Xero has approved, computed fresh from the portal's own tables on every query — never
/// stored against the Xero months, so the synced rows stay pure Xero and always auditable.
/// </summary>
public sealed class GetXeroSitePnlHandler : IQueryHandler<GetXeroSitePnl, XeroSitePnlSnapshot>
{
    // Bill statuses whose lines are inside Xero's P&L reports. DRAFT and SUBMITTED are not:
    // a draft bill (Dext-published or staged by the coding run) is invisible to the reports,
    // which is exactly why the labour it settles must accrue until approval.
    private static readonly string[] SettledInvoiceStatuses = { "AUTHORISED", "PAID" };

    private readonly JpmsContext context;
    private readonly IXeroClient xero;

    public GetXeroSitePnlHandler(JpmsContext context, IXeroClient xero)
    {
        this.context = context;
        this.xero = xero;
    }

    public async Task<XeroSitePnlSnapshot> HandleAsync(GetXeroSitePnl query, CancellationToken cancellationToken)
    {
        var rows = await context.XeroSitePnlMonths
            .OrderBy(row => row.ProjectId)
            .ThenBy(row => row.Month)
            .Select(row => new XeroSiteMonthlyPnl(
                row.ProjectId, row.Month, row.Income, row.CostOfSales, row.OperatingExpenses))
            .ToListAsync(cancellationToken);

        // Null until the first sync lands — the UI reads that as "never synced", not "empty".
        var lastSynced = await context.XeroSitePnlMonths
            .MaxAsync(row => (DateTimeOffset?)row.LastSyncedAtUtc, cancellationToken);

        var accruals = await LabourAccrualsAsync(cancellationToken);

        return new XeroSitePnlSnapshot(xero.IsConfigured, lastSynced, rows, accruals);
    }

    // Approved-but-unsettled labour per project per month (worked date), the Profit Summary's
    // accrual overlay (Labour-Overview-Forecast-and-Xero-Mapping-Scope §6). A timesheet is
    // SETTLED — inside Xero's own P&L, so it must not accrue — when a timesheet-cover for its
    // project and its worker's subcontractor spans its worked date AND the covering bill is
    // approved in Xero. A cover on a still-draft bill does not settle: Xero's reports ignore
    // drafts. A worker with no linked subcontractor can never match a cover, so their approved
    // time accrues until the link is made — the same honest gap the coding run reports rather
    // than guesses around. Settlement variances are deliberately not accrued: a variance exists
    // only once a bill is being reconciled, and when that bill is approved both it and its
    // timesheets leave the accrual together.
    private async Task<IReadOnlyList<XeroSiteMonthlyLabourAccrual>> LabourAccrualsAsync(CancellationToken cancellationToken)
    {
        var approved = await context.Timesheets.AsNoTracking()
            .Where(timesheet => timesheet.Status == (int)TimesheetStatus.Approved && timesheet.CostAmount != 0m)
            .Select(timesheet => new { timesheet.ProjectId, timesheet.WorkerId, timesheet.WorkedOn, timesheet.CostAmount })
            .ToListAsync(cancellationToken);
        if (approved.Count == 0) return Array.Empty<XeroSiteMonthlyLabourAccrual>();

        // Keyed by settlement COUNTERPARTY (2026-08-31): the linked company, or the worker
        // themself when flagged a sole trader — covers are stored against that id, so a sole
        // trader's approved time leaves the accrual when their own-name bill is approved,
        // exactly like a company-linked worker's.
        var subcontractorByWorker = (await context.Workers.AsNoTracking()
                .Where(worker => worker.SubcontractorId != null || worker.IsSoleTrader)
                .Select(worker => new { worker.WorkerId, worker.SubcontractorId, worker.IsSoleTrader })
                .ToListAsync(cancellationToken))
            .ToDictionary(
                worker => worker.WorkerId,
                worker => worker.SubcontractorId ?? worker.WorkerId,
                StringComparer.OrdinalIgnoreCase);

        var settledSpans = (await context.XeroLineTimesheetCovers.AsNoTracking()
                .Join(context.XeroLedgerLines.AsNoTracking(),
                    cover => cover.XeroLedgerLineId,
                    line => line.XeroLedgerLineId,
                    (cover, line) => new
                    {
                        cover.ProjectId,
                        cover.SubcontractorId,
                        cover.PeriodStart,
                        cover.PeriodEnd,
                        line.InvoiceStatus,
                    })
                .Where(entry => SettledInvoiceStatuses.Contains(entry.InvoiceStatus))
                .ToListAsync(cancellationToken))
            .ToLookup(entry => SpanKey(entry.ProjectId, entry.SubcontractorId), StringComparer.OrdinalIgnoreCase);

        var totals = new Dictionary<(string ProjectId, DateTime Month), decimal>();
        foreach (var timesheet in approved)
        {
            var settled = subcontractorByWorker.TryGetValue(timesheet.WorkerId, out var subcontractorId)
                && settledSpans[SpanKey(timesheet.ProjectId, subcontractorId)]
                    .Any(span => span.PeriodStart <= timesheet.WorkedOn && timesheet.WorkedOn < span.PeriodEnd);
            if (settled) continue;

            var workedDate = timesheet.WorkedOn.UtcDateTime.Date;
            var month = new DateTime(workedDate.Year, workedDate.Month, 1);
            totals[(timesheet.ProjectId, month)] =
                totals.TryGetValue((timesheet.ProjectId, month), out var runningTotal)
                    ? runningTotal + timesheet.CostAmount
                    : timesheet.CostAmount;
        }

        return totals
            .OrderBy(entry => entry.Key.ProjectId)
            .ThenBy(entry => entry.Key.Month)
            .Select(entry => new XeroSiteMonthlyLabourAccrual(
                entry.Key.ProjectId, entry.Key.Month, Math.Round(entry.Value, 2)))
            .ToList();
    }

    // Ids come from the same database but are compared the way every project/subcontractor id
    // comparison is: case-insensitively. A newline can't occur in either id, so keys are unambiguous.
    private static string SpanKey(string projectId, string subcontractorId) => projectId + "\n" + subcontractorId;
}
