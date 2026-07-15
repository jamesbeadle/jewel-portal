using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Xero;

// ============================================================================
// Xero ledger allocation — reconciling accounts (Xero) with projects (JPMS).
// Purchase invoice LINES pulled from Xero are stored in JPMS with an
// allocation status; each line is allocated to a JPMS project + master cost
// centre (00001..00137 — deliberately independent of Xero's own tracking
// codes). Syncing upserts by Xero line id: new lines arrive Unallocated and
// existing lines have their Xero facts refreshed without ever touching the
// allocation. A line is allocated to ONE project, but its value can be SPLIT
// across multiple cost centres (Splits — net amounts summing to the line net).
//
// Write-back: bills arrive from Dext as DRAFT. Once every stored line of a
// draft (or submitted) bill is allocated, JPMS writes the allocation back to
// Xero — Sites + Cost Code tracking per line, splitting Xero lines where the
// allocation is split — and approves the invoice (DRAFT → AUTHORISED). Bills
// already approved outside JPMS are still allocated portal-side only.
// ============================================================================

public enum XeroAllocationStatus { Unallocated = 0, Allocated = 1, Ignored = 2, Bucketed = 3 }

/// <summary>
/// SetProject is the half-step before Allocate: it persists the project on a
/// line that STAYS Unallocated (so it sits in that project's queue awaiting a
/// cost centre) and best-effort writes the project's Site tracking to Xero
/// without approving the bill. The line leaves the queue only via Allocate.
/// </summary>
public enum XeroAllocationAction { Allocate = 0, Ignore = 1, Reset = 2, AllocateToBucket = 3, SetProject = 4 }

/// <summary>
/// Outcome of the last attempt to write an invoice's allocation back to Xero
/// (tracking + approval). None: never attempted — either the invoice was
/// already approved outside JPMS (no write-back needed) or its other lines are
/// still awaiting allocation. Failed lines keep their JPMS allocation; the
/// error is stored and the write-back can be retried.
/// </summary>
public enum XeroWriteBackStatus { None = 0, Approved = 1, Failed = 2 }

/// <summary>
/// One share of a ledger line: a cost centre, the net amount (pre-VAT and
/// positive, like the line's Net) and the project the share belongs to — a
/// split can span projects as well as cost centres. A null ProjectId falls
/// back to the command/line-level project.
/// </summary>
public sealed record XeroCostSplit(string CostCenterCode, decimal Net, string? ProjectId = null);

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
    public const string Tolls = "Tolls";
    public const string Travel = "Travel";
    public const string Software = "Software subscriptions";
    public const string Ica = "ICA (Intercompany Account)";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All = new[] { Parking, Fuel, Tolls, Travel, Software, Ica, Other };
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
    DateTimeOffset LastSyncedAtUtc,
    // Split for allocated lines. Null/empty = the whole line sits on this line's
    // ProjectId + CostCenterCode; entries = the line's net is shared across those
    // projects/centres (CostCenterCode is then null, ProjectId holds the common
    // project or null when the split spans projects, and the split nets sum to Net).
    IReadOnlyList<XeroCostSplit>? Splits = null,
    XeroWriteBackStatus WriteBackStatus = XeroWriteBackStatus.None,
    string? WriteBackError = null,
    DateTimeOffset? WriteBackAtUtc = null,
    // Whether Xero holds attachments for this line's invoice (the supplier's
    // document, published by Dext) — arms the invoice viewer on the allocation
    // page. Refreshed on every sync like the other Xero facts.
    bool HasAttachments = false);

/// <summary>
/// The attachments Xero holds for one purchase invoice or credit note — the
/// supplier's actual document(s), listed live from Xero (nothing stored in
/// JPMS). <paramref name="IsCreditNote"/> picks Xero's CreditNotes collection
/// (line Type ACCPAYCREDIT) over Invoices.
/// </summary>
public sealed record ListXeroInvoiceAttachments(string XeroInvoiceId, bool IsCreditNote = false)
    : IQuery<IReadOnlyList<XeroInvoiceAttachment>>;

/// <summary>One attachment as Xero holds it; the bytes are streamed on demand by file name.</summary>
public sealed record XeroInvoiceAttachment(
    string AttachmentId,
    string FileName,
    string MimeType,
    long ContentLength);

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
/// either ProjectId + CostCenterCode (whole line to one project + centre) or
/// Splits — two or more shares, each with its own project and cost centre,
/// whose nets must sum exactly to the line's net (splits therefore apply to a
/// single line, never a batch). A split entry without a ProjectId falls back
/// to the command's ProjectId. AllocateToBucket requires a Bucket from
/// <see cref="XeroBuckets.All"/>; Ignore takes an optional Note (reason);
/// Reset returns lines to Unallocated. SetProject requires ProjectId only and
/// applies to Unallocated lines: the project is saved (line stays Unallocated,
/// queued under that project) and its Xero Site tracking is written without
/// approving the bill. AllocatedBy is stamped server-side from the signed-in
/// user — any client-supplied value is ignored.
/// </summary>
public sealed record SetXeroAllocation(
    IReadOnlyList<string> XeroLedgerLineIds,
    XeroAllocationAction Action,
    string? ProjectId = null,
    string? CostCenterCode = null,
    string? Bucket = null,
    string? Note = null,
    string? AllocatedBy = null,
    IReadOnlyList<XeroCostSplit>? Splits = null) : ICommand<int>;

/// <summary>
/// Re-attempts the Xero write-back (tracking + approval) for one invoice whose
/// previous attempt failed — e.g. an unmapped Sites option or a Xero outage.
/// Succeeds silently when the invoice has since been approved in Xero.
/// </summary>
public sealed record RetryXeroWriteBack(string XeroInvoiceId) : ICommand<XeroWriteBackOutcome>;

public sealed record XeroWriteBackOutcome(bool Succeeded, string? Error);
