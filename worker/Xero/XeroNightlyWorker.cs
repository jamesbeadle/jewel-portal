using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Worker.Xero;

/// <summary>
/// Nightly Xero housekeeping, so the allocation queue is current before anyone
/// sits down in the morning: pulls the latest purchase invoices + credit notes
/// into the stored ledger (SyncXeroLedgerHandler), then allocates every
/// unallocated line whose Sites + Cost Code tracking fully resolved a project
/// and cost centre (AllocateSuggestedXeroLinesHandler) — which also confirms
/// and approves any draft bill those allocations completed (DRAFT → AUTHORISED,
/// the same best-effort write-back a human triggers from the allocation page).
/// Partially-matched lines stay in the queue for a human; auto-matched lines
/// carry the standard note so they remain identifiable and bulk-reversible.
///
/// Runs the identical handlers the API's HTTP endpoints use (linked source), so
/// the overnight run and the page's Sync / "Allocate all matched" buttons are
/// one code path. Failures are logged and the next night retries — the sync is
/// a full upsert from Xero's current state, so a missed night self-heals.
///
/// Since 2026-09-11 it also reads the SALES side back: for every project with a Xero
/// contact mapped, the same SyncValuationInvoicePaymentsFromXero the invoices section's
/// "Sync payments from Xero…" runs — an issued valuation invoice Xero holds as PAID is
/// recorded paid here, and one keyed into Xero by hand is linked when the match is
/// unique. Xero is the home of what has been paid; the portal reads it every night.
/// </summary>
public sealed class XeroNightlyWorker
{
    /// <summary>
    /// Stamped as AllocatedBy on lines the nightly run allocates — an endpoint
    /// stamps the signed-in user's email here, so this marks "no human chose
    /// this" wherever the allocator is shown.
    /// </summary>
    public const string NightlyActor = "Nightly auto-match";

    private readonly ICommandHandler<SyncXeroLedger, XeroLedgerSyncResult> sync;
    private readonly ICommandHandler<AllocateSuggestedXeroLines, int> allocate;
    private readonly ICommandHandler<SyncXeroSitePnl, XeroSitePnlSyncResult> sitePnl;
    private readonly ICommandHandler<SyncValuationInvoicePaymentsFromXero, ValuationInvoicePaymentSyncOutcome> paymentSync;
    private readonly JpmsContext db;
    private readonly IXeroClient xero;
    private readonly ILogger<XeroNightlyWorker> logger;

    public XeroNightlyWorker(
        ICommandHandler<SyncXeroLedger, XeroLedgerSyncResult> sync,
        ICommandHandler<AllocateSuggestedXeroLines, int> allocate,
        ICommandHandler<SyncXeroSitePnl, XeroSitePnlSyncResult> sitePnl,
        ICommandHandler<SyncValuationInvoicePaymentsFromXero, ValuationInvoicePaymentSyncOutcome> paymentSync,
        JpmsContext db,
        IXeroClient xero,
        ILogger<XeroNightlyWorker> logger)
    {
        this.sync = sync;
        this.allocate = allocate;
        this.sitePnl = sitePnl;
        this.paymentSync = paymentSync;
        this.db = db;
        this.xero = xero;
        this.logger = logger;
    }

    // 04:30 UTC daily — 05:30 UK in summer, 04:30 in winter (NCRONTAB is evaluated in UTC on
    // Linux Function Apps): always before the working day starts, and after Dext's overnight
    // publish of transcribed bills into Xero as drafts.
    [Function(nameof(XeroNightlyWorker))]
    public async Task Run([TimerTrigger("0 30 4 * * *")] TimerInfo timer, CancellationToken ct)
    {
        if (!xero.IsConfigured)
        {
            logger.LogInformation("Nightly Xero run skipped: Xero credentials are not configured on this app.");
            return;
        }

        var result = await sync.HandleAsync(new SyncXeroLedger(), ct);
        if (!result.IsConfigured || result.Error is not null)
        {
            // Xero said no (or the client lost its config): nothing was written, nothing to
            // allocate. Tomorrow's run retries from Xero's current state.
            logger.LogWarning("Nightly Xero sync did not complete: {Error}", result.Error ?? "not configured");
            return;
        }

        logger.LogInformation(
            "Nightly Xero sync: {New} new, {Updated} refreshed, {Removed} removed — {Total} stored lines, {Unallocated} awaiting allocation.",
            result.NewLines, result.UpdatedLines, result.RemovedLines, result.TotalLines, result.UnallocatedLines);

        var allocated = await allocate.HandleAsync(new AllocateSuggestedXeroLines(NightlyActor), ct);
        logger.LogInformation(
            "Nightly Xero auto-allocation: {Allocated} fully-matched line(s) allocated (write-back attempted per completed draft bill); the rest await a human.",
            allocated);

        // Site P&L (the Profit Summary's cumulative invoiced-vs-cost chart): re-read every
        // mapped project's monthly figures from Xero's P&L report — FULL history, because
        // this run faces no HTTP gateway and is where deep recodes self-heal (the page's
        // Refresh button only re-reads the recent window). A failure here is logged and
        // left for tomorrow — it must not stop the ledger sync above having landed.
        var pnl = await sitePnl.HandleAsync(new SyncXeroSitePnl(FullHistory: true), ct);
        if (pnl.Error is not null)
            logger.LogWarning("Nightly site P&L sync did not complete: {Error}", pnl.Error);
        else
            logger.LogInformation(
                "Nightly site P&L sync: {Projects} project(s) refreshed, {Months} month rows stored{Unmapped}.",
                pnl.ProjectsSynced, pnl.MonthsStored,
                pnl.UnmappedProjectNames.Count > 0
                    ? $" ({pnl.UnmappedProjectNames.Count} project(s) have no Xero site mapping)"
                    : "");

        // The accountant's acceptance test, nightly: stored months vs Xero's whole-range site
        // P&L on the same pull. A mismatch is a warning with the figures — never silent.
        foreach (var check in pnl.Reconciliations ?? Array.Empty<XeroSitePnlReconciliation>())
        {
            if (check.Matches) continue;
            logger.LogWarning(
                "Site P&L reconciliation mismatch for {Project} ({From:yyyy-MM-dd}→{To:yyyy-MM-dd}): "
                + "stored income {StoredIncome:0.00} / cost of sales {StoredCos:0.00} / opex {StoredOpex:0.00} "
                + "vs Xero {XeroIncome:0.00} / {XeroCos:0.00} / {XeroOpex:0.00}.",
                check.ProjectName, check.FromDate, check.ToDate,
                check.StoredIncome, check.StoredCostOfSales, check.StoredOperatingExpenses,
                check.XeroIncome, check.XeroCostOfSales, check.XeroOperatingExpenses);
        }

        await SyncValuationInvoicePaymentsAsync(ct);
    }

    /// <summary>
    /// The sales side read back, per mapped project: what Xero holds as PAID becomes Paid here,
    /// through the one payment handler. One project's failure (Xero refusing, a refusal from the
    /// handler) is logged and the rest still run — the change tracker is cleared so a half-saved
    /// project never rides into the next one.
    /// </summary>
    private async Task SyncValuationInvoicePaymentsAsync(CancellationToken ct)
    {
        var projects = await db.Projects.AsNoTracking()
            .Where(project => project.XeroContactId != null && project.XeroContactId != "")
            .OrderBy(project => project.Name)
            .Select(project => new { project.ProjectId, project.Name })
            .ToListAsync(ct);
        if (projects.Count == 0)
        {
            logger.LogInformation("Nightly valuation-invoice payment sync: no project has a Xero contact mapped — nothing to read.");
            return;
        }

        int recorded = 0, linked = 0, failedProjects = 0;
        foreach (var project in projects)
        {
            try
            {
                var outcome = await paymentSync.HandleAsync(new SyncValuationInvoicePaymentsFromXero(project.ProjectId), ct);
                recorded += outcome.PaymentsRecorded;
                linked += outcome.Linked;
                if (outcome.PaymentsRecorded > 0 || outcome.Linked > 0 || outcome.Failed > 0)
                    logger.LogInformation(
                        "Nightly valuation-invoice payment sync for {Project}: {Paid} payment(s) recorded, {Linked} linked, {Unchanged} unchanged, {Failed} refused{Detail}.",
                        project.Name, outcome.PaymentsRecorded, outcome.Linked, outcome.NoChange, outcome.Failed,
                        outcome.Failed > 0
                            ? " — " + string.Join("; ", outcome.Results.Where(r => r.Error is not null).Select(r => $"{r.Row.Reference}: {r.Error}"))
                            : "");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception failure)
            {
                failedProjects++;
                db.ChangeTracker.Clear();
                logger.LogWarning(failure, "Nightly valuation-invoice payment sync for {Project} did not complete: {Error}", project.Name, failure.Message);
            }
        }

        logger.LogInformation(
            "Nightly valuation-invoice payment sync: {Projects} project(s) read, {Paid} payment(s) recorded, {Linked} linked, {FailedProjects} project(s) failed.",
            projects.Count, recorded, linked, failedProjects);
    }
}
