using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// Pulls the latest purchase invoices + credit notes from Xero (force — bypasses
/// the read cache) and upserts each LINE into XeroLedgerLines. New lines arrive
/// Unallocated; existing lines get their Xero facts refreshed while the JPMS
/// allocation fields are left untouched. Lines Xero no longer returns (e.g. a
/// bill edited to remove a line) simply stop being refreshed — their older
/// LastSyncedAtUtc makes them identifiable without destroying an allocation.
/// The exception is voided/deleted invoices: voiding reverses the cost, so their
/// stored lines are removed even when already allocated (splits included), taking
/// them out of project financials on the next sync.
/// </summary>
public sealed class SyncXeroLedgerHandler : ICommandHandler<SyncXeroLedger, XeroLedgerSyncResult>
{
    private readonly IXeroClient xero;
    private readonly JpmsContext context;
    private readonly XeroOptions options;

    public SyncXeroLedgerHandler(IXeroClient xero, JpmsContext context, XeroOptions options)
    {
        this.xero = xero;
        this.context = context;
        this.options = options;
    }

    /// <summary>
    /// Only cost-of-sales lines are allocated to projects: the nominal account the
    /// line posts to must start with a configured prefix (default "3"). Lines with
    /// no account code (stripped/deleted invoices) are not cost of sales.
    /// </summary>
    private bool IsCostOfSales(string? accountCode)
    {
        if (options.CostOfSalesAccountPrefixes.Count == 0) return true;
        if (string.IsNullOrWhiteSpace(accountCode)) return false;
        return options.CostOfSalesAccountPrefixes.Any(prefix =>
            accountCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    // Statuses that never enter the allocation queue: voided/deleted bills aren't
    // costs at all. DRAFT bills DO enter — Dext publishes transcribed purchase
    // invoices into Xero as drafts, and allocating a draft here is what confirms
    // its cost codes back to Xero and approves it (DRAFT → AUTHORISED). SUBMITTED,
    // AUTHORISED and PAID count as before.
    private static readonly HashSet<string> ExcludedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "VOIDED", "DELETED"
    };

    public async Task<XeroLedgerSyncResult> HandleAsync(SyncXeroLedger command, CancellationToken cancellationToken)
    {
        // force:false — a snapshot fetched within the cache window (e.g. by the Xero
        // transactions page moments earlier) is reused rather than re-pulling dozens of
        // Xero pages; a cold cache still triggers a full pull. Keeps sync well inside
        // the platform's HTTP timeout.
        var snapshot = await xero.GetPurchaseInvoicesAsync(force: false, cancellationToken);
        if (!snapshot.IsConfigured)
            return new XeroLedgerSyncResult(false, null, 0, 0, 0, 0, 0);
        if (snapshot.Error is not null)
            return new XeroLedgerSyncResult(true, snapshot.Error, 0, 0, 0, 0, 0);

        var existingById = await context.XeroLedgerLines
            .ToDictionaryAsync(line => line.XeroLedgerLineId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        int added = 0, updated = 0, removed = 0;
        var seenLineIds = new HashSet<string>();

        foreach (var transaction in snapshot.Transactions)
        {
            var excluded = ExcludedStatuses.Contains(transaction.Status);

            foreach (var line in transaction.Lines)
            {
                // Without a stable line id there is nothing safe to upsert on; Xero
                // returns LineItemID on every persisted line, so this is defensive.
                if (string.IsNullOrWhiteSpace(line.LineItemId)) continue;

                var id = $"{transaction.TransactionId}:{line.LineItemId}";

                if (excluded)
                {
                    // Never add voided/deleted to the queue. (Stored lines for these
                    // invoices are cleaned up in the invoice-level pass below — deleted
                    // invoices usually come back with NO line items at all, so a per-line
                    // check here would miss them.)
                    continue;
                }

                // Only cost-of-sales lines (nominal account 3xx by default) are queued for
                // allocation; overhead lines are skipped. Existing stored lines that fail
                // the rule are cleaned up in the pass below.
                if (!IsCostOfSales(line.AccountCode)) continue;

                seenLineIds.Add(id);

                if (existingById.TryGetValue(id, out var entity))
                {
                    updated++;
                }
                else
                {
                    entity = new XeroLedgerLineEntity
                    {
                        XeroLedgerLineId = id,
                        XeroInvoiceId = transaction.TransactionId,
                        XeroLineItemId = line.LineItemId!,
                        AllocationStatus = (int)XeroAllocationStatus.Unallocated,
                        FirstSeenAtUtc = now
                    };
                    context.XeroLedgerLines.Add(entity);
                    existingById[id] = entity;
                    added++;
                }

                // Xero-owned facts — refreshed every sync. Allocation fields untouched.
                // Every string is clamped to its column size: Xero allows longer values
                // (references up to 255 chars etc.) and one oversized value would fail
                // the whole SaveChanges with a truncation error.
                entity.Type = Truncate(transaction.Type, 16)!;
                entity.InvoiceNumber = Truncate(transaction.Number, 64);
                entity.Reference = Truncate(transaction.Reference, 256);
                entity.ContactName = Truncate(transaction.ContactName, 256);
                entity.Date = transaction.Date;
                entity.InvoiceStatus = Truncate(transaction.Status, 32)!;
                entity.Description = Truncate(line.Description, 1024);
                entity.Net = line.LineAmount;
                entity.Tax = line.TaxAmount;
                entity.AccountCode = Truncate(line.AccountCode, 32);
                entity.AccountName = Truncate(line.AccountName, 256);
                entity.XeroSite = Truncate(line.Site, 128);
                entity.XeroCostCode = Truncate(line.CostCode, 128);
                entity.HasAttachments = transaction.HasAttachments;
                entity.LastSyncedAtUtc = now;
            }
        }

        // Invoice-level cleanup pass. Deleted invoices come back from Xero with no
        // line items (so the loop above never sees them), and lines can be edited
        // off a bill entirely.
        //   * stored lines of voided/deleted invoices -> removed, allocated or not.
        //     Voiding reverses the cost, so the allocation (and any split shares)
        //     must stop counting in project financials rather than linger flagged.
        //   * unallocated stored lines that no longer exist on the bill -> removed
        //   * other allocated/ignored lines are kept untouched.
        var statusByInvoiceId = snapshot.Transactions
            .GroupBy(transaction => transaction.TransactionId)
            .ToDictionary(group => group.Key, group => group.First().Status);

        var removedLineIds = new List<string>();
        foreach (var entity in existingById.Values.ToList())
        {
            var snapshotStatus = statusByInvoiceId.TryGetValue(entity.XeroInvoiceId, out var status) ? status : null;

            // Voided/deleted reverses the cost regardless of allocation state. The stored
            // status covers invoices that have dropped out of the snapshot window since
            // they were last refreshed.
            if ((snapshotStatus is not null && ExcludedStatuses.Contains(snapshotStatus))
                || ExcludedStatuses.Contains(entity.InvoiceStatus))
            {
                context.XeroLedgerLines.Remove(entity);
                removedLineIds.Add(entity.XeroLedgerLineId);
                removed++;
                continue;
            }

            if (entity.AllocationStatus == (int)XeroAllocationStatus.Unallocated)
            {
                // The account rule holds regardless of whether the invoice was in this
                // snapshot — an unallocated non-cost-of-sales line never belongs here.
                if (!IsCostOfSales(entity.AccountCode))
                {
                    context.XeroLedgerLines.Remove(entity);
                    removed++;
                    continue;
                }

                if (snapshotStatus is not null && !seenLineIds.Contains(entity.XeroLedgerLineId))
                {
                    context.XeroLedgerLines.Remove(entity);
                    removed++;
                }
            }
        }

        // A removed line's split shares, work-order link slices and package cost slices
        // go with it — orphans would silently keep feeding the per-centre actuals,
        // per-order invoiced balances and package figures the removal is meant to reverse.
        if (removedLineIds.Count > 0)
        {
            var orphanedSplits = await context.XeroCostSplits
                .Where(split => removedLineIds.Contains(split.XeroLedgerLineId))
                .ToListAsync(cancellationToken);
            context.XeroCostSplits.RemoveRange(orphanedSplits);

            var orphanedLinks = await context.XeroLineWorkOrderLinks
                .Where(link => removedLineIds.Contains(link.XeroLedgerLineId))
                .ToListAsync(cancellationToken);
            context.XeroLineWorkOrderLinks.RemoveRange(orphanedLinks);

            var orphanedPackageCosts = await context.ReconciliationPackageCostLines
                .Where(slice => removedLineIds.Contains(slice.XeroLedgerLineId))
                .ToListAsync(cancellationToken);
            context.ReconciliationPackageCostLines.RemoveRange(orphanedPackageCosts);
        }

        await context.SaveChangesAsync(cancellationToken);

        var total = await context.XeroLedgerLines.CountAsync(cancellationToken);
        var unallocated = await context.XeroLedgerLines
            .CountAsync(line => line.AllocationStatus == (int)XeroAllocationStatus.Unallocated, cancellationToken);

        return new XeroLedgerSyncResult(true, null, added, updated, removed, total, unallocated);
    }

    private static string? Truncate(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];
}
