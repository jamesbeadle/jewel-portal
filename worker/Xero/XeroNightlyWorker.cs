using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Xero;
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
    private readonly IXeroClient xero;
    private readonly ILogger<XeroNightlyWorker> logger;

    public XeroNightlyWorker(
        ICommandHandler<SyncXeroLedger, XeroLedgerSyncResult> sync,
        ICommandHandler<AllocateSuggestedXeroLines, int> allocate,
        IXeroClient xero,
        ILogger<XeroNightlyWorker> logger)
    {
        this.sync = sync;
        this.allocate = allocate;
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
    }
}
