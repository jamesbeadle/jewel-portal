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
/// </summary>
public sealed class SyncXeroLedgerHandler : ICommandHandler<SyncXeroLedger, XeroLedgerSyncResult>
{
    private readonly IXeroClient xero;
    private readonly JpmsContext context;

    public SyncXeroLedgerHandler(IXeroClient xero, JpmsContext context)
    {
        this.xero = xero;
        this.context = context;
    }

    public async Task<XeroLedgerSyncResult> HandleAsync(SyncXeroLedger command, CancellationToken cancellationToken)
    {
        // force:false — a snapshot fetched within the cache window (e.g. by the Xero
        // transactions page moments earlier) is reused rather than re-pulling dozens of
        // Xero pages; a cold cache still triggers a full pull. Keeps sync well inside
        // the platform's HTTP timeout.
        var snapshot = await xero.GetPurchaseInvoicesAsync(force: false, cancellationToken);
        if (!snapshot.IsConfigured)
            return new XeroLedgerSyncResult(false, null, 0, 0, 0, 0);
        if (snapshot.Error is not null)
            return new XeroLedgerSyncResult(true, snapshot.Error, 0, 0, 0, 0);

        var existingById = await context.XeroLedgerLines
            .ToDictionaryAsync(line => line.XeroLedgerLineId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        int added = 0, updated = 0;

        foreach (var transaction in snapshot.Transactions)
        {
            foreach (var line in transaction.Lines)
            {
                // Without a stable line id there is nothing safe to upsert on; Xero
                // returns LineItemID on every persisted line, so this is defensive.
                if (string.IsNullOrWhiteSpace(line.LineItemId)) continue;

                var id = $"{transaction.TransactionId}:{line.LineItemId}";
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
                entity.LastSyncedAtUtc = now;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var total = await context.XeroLedgerLines.CountAsync(cancellationToken);
        var unallocated = await context.XeroLedgerLines
            .CountAsync(line => line.AllocationStatus == (int)XeroAllocationStatus.Unallocated, cancellationToken);

        return new XeroLedgerSyncResult(true, null, added, updated, total, unallocated);
    }

    private static string? Truncate(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];
}
