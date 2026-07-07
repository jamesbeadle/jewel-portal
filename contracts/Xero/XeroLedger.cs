using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Xero;

// ============================================================================
// Xero ledger allocation — reconciling accounts (Xero) with projects (JPMS).
// Purchase invoice LINES pulled from Xero are stored in JPMS with an
// allocation status; each line is allocated to a JPMS project + master cost
// centre (00001..00137 — deliberately independent of Xero's own tracking
// codes). Syncing upserts by Xero line id: new lines arrive Unallocated and
// existing lines have their Xero facts refreshed without ever touching the
// allocation. One line carries exactly one allocation (no splits).
// ============================================================================

public enum XeroAllocationStatus { Unallocated = 0, Allocated = 1, Ignored = 2, Bucketed = 3 }

public enum XeroAllocationAction { Allocate = 0, Ignore = 1, Reset = 2, AllocateToBucket = 3 }

/// <summary>
/// Buckets for cost-of-sales lines with no identifiable project (parking charges,
/// fuel, software subscriptions...). Bucketed spend stays visible with per-bucket
/// totals so it can be drilled into and dealt with internally, while the
/// allocation queue clears down to genuine project costs.
/// </summary>
public static class XeroBuckets
{
    public const string Parking = "Parking";
    public const string Fuel = "Fuel";
    public const string Software = "Software subscriptions";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All = new[] { Parking, Fuel, Software, Other };
}

/// <summary>All stored ledger lines with allocation state and server-computed suggestions.</summary>
public sealed record ListXeroLedgerLines : IQuery<IReadOnlyList<XeroLedgerLine>>;

/// <summary>
/// One stored Xero purchase-invoice line. Amounts are net (pre-VAT, normalised
/// for VAT-inclusive invoices) and positive; <see cref="Type"/> distinguishes
/// bills (ACCPAY) from supplier credit notes (ACCPAYCREDIT), which subtract in
/// any spend view. Suggested* fields are the server's best guess from the
/// line's Xero Sites / Cost Code tracking — never applied automatically.
/// </summary>
public sealed record XeroLedgerLine(
    string XeroLedgerLineId,
    string XeroInvoiceId,
    string Type,
    string? InvoiceNumber,
    string? Reference,
    string? ContactName,
    DateTime? Date,
    string InvoiceStatus,
    string? Description,
    decimal Net,
    decimal Tax,
    string? AccountCode,
    string? AccountName,
    string? XeroSite,
    string? XeroCostCode,
    XeroAllocationStatus AllocationStatus,
    string? ProjectId,
    string? CostCenterCode,
    string? Bucket,
    string? AllocatedBy,
    DateTimeOffset? AllocatedAtUtc,
    string? Note,
    string? SuggestedProjectId,
    string? SuggestedCostCenterCode,
    string? SuggestedBucket,
    DateTimeOffset FirstSeenAtUtc,
    DateTimeOffset LastSyncedAtUtc);

/// <summary>
/// Pulls the latest purchase invoices + credit notes from Xero (bypassing the
/// read cache) and upserts them into the stored ledger. Allocations survive.
/// </summary>
public sealed record SyncXeroLedger : ICommand<XeroLedgerSyncResult>;

public sealed record XeroLedgerSyncResult(
    bool IsConfigured,
    string? Error,
    int NewLines,
    int UpdatedLines,
    int RemovedLines,
    int TotalLines,
    int UnallocatedLines);

/// <summary>
/// Allocates every unallocated line whose suggestions resolved BOTH a project
/// and a cost centre (recomputed server-side at execution time, so what gets
/// applied is exactly what the queue shows as pre-filled). Allocations are
/// noted as auto-matched so they can be found and reviewed later. Returns how
/// many lines were allocated.
/// </summary>
public sealed record AllocateSuggestedXeroLines(string? AllocatedBy = null) : ICommand<int>;

/// <summary>
/// Applies one allocation action to a batch of ledger lines. Allocate requires
/// ProjectId + CostCenterCode; AllocateToBucket requires a Bucket from
/// <see cref="XeroBuckets.All"/>; Ignore takes an optional Note (reason); Reset
/// returns lines to Unallocated. AllocatedBy is stamped server-side from the
/// signed-in user — any client-supplied value is ignored.
/// </summary>
public sealed record SetXeroAllocation(
    IReadOnlyList<string> XeroLedgerLineIds,
    XeroAllocationAction Action,
    string? ProjectId = null,
    string? CostCenterCode = null,
    string? Bucket = null,
    string? Note = null,
    string? AllocatedBy = null) : ICommand<int>;
