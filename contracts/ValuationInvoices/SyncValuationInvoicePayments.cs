using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ValuationInvoices;

// Payments read BACK from Xero (2026-09-11, the MD's ask: "the portal says it can't recognise when
// a sales invoice is paid"). Xero is the home of what has been paid; the portal READS it rather
// than being told. One rule plans both the preview a person confirms and the sync that follows
// (and the nightly worker's unattended run): for every ISSUED valuation invoice on a project, the
// Xero sales invoice it is linked to is read fresh — PAID there means Paid here — and one with no
// Xero link yet is matched to the contact's invoices the way a person would (a reference naming
// the valuation, or the same net amount). A unique match is applied; two candidates are never
// guessed between — the row says so and the pairing is made by hand
// (RecordValuationInvoiceXeroNumber) before the next sync.

/// <summary>What the sync would do for one project, read fresh from Xero. Nothing is written.</summary>
public sealed record PreviewValuationInvoicePaymentSync(string ProjectId) : IQuery<ValuationInvoicePaymentSyncPreview>;

/// <summary>
/// Applies the plan the preview shows: links matched invoices (Xero id + number stamped) and records
/// the payment on every one Xero holds as PAID, through the same RecordValuationInvoicePayment move
/// the row's "Record payment…" runs, with PaidAt set to Xero's fully-paid date. SyncedBy is stamped
/// server-side from the signed-in user; the nightly worker leaves it null.
/// </summary>
public sealed record SyncValuationInvoicePaymentsFromXero(string ProjectId, string? SyncedBy = null)
    : ICommand<ValuationInvoicePaymentSyncOutcome>;

public enum ValuationInvoicePaymentSyncAction
{
    /// <summary>Nothing to do — Note says why (unpaid, part paid, no match, ambiguous, gone from Xero).</summary>
    None = 0,
    /// <summary>Stamp the matched Xero invoice's id and number onto the portal invoice; Xero still shows it unpaid.</summary>
    Link = 1,
    /// <summary>The linked Xero invoice is PAID — record the payment here.</summary>
    RecordPayment = 2,
    /// <summary>Both: the match is unique and Xero shows it PAID.</summary>
    LinkAndRecordPayment = 3
}

/// <summary>
/// One Issued valuation invoice and what the sync plans for it. Amount is the portal's net; the
/// payment recorded is that net (the portal's paid convention, compared with certified totals) —
/// Note carries the cross-check when Xero's net differs. Candidates lists the Xero invoices an
/// ambiguous row could be, for the person to choose between.
/// </summary>
public sealed record ValuationInvoicePaymentSyncRow(
    string ValuationInvoiceId,
    string Reference,
    ValuationInvoiceStatus Status,
    decimal Amount,
    string? XeroInvoiceId,
    string? XeroInvoiceNumber,
    string? XeroStatus,
    DateTime? XeroDate,
    ValuationInvoicePaymentSyncAction Action,
    decimal? AmountToRecord,
    DateTime? PaidOn,
    string? Note,
    IReadOnlyList<string> Candidates)
{
    public bool Links => Action is ValuationInvoicePaymentSyncAction.Link or ValuationInvoicePaymentSyncAction.LinkAndRecordPayment;
    public bool RecordsPayment => Action is ValuationInvoicePaymentSyncAction.RecordPayment or ValuationInvoicePaymentSyncAction.LinkAndRecordPayment;
}

public sealed record ValuationInvoicePaymentSyncPreview(
    string ProjectId,
    string ProjectName,
    string? XeroContactId,
    string? XeroContactName,
    DateTimeOffset? FetchedAtUtc,
    IReadOnlyList<ValuationInvoicePaymentSyncRow> Rows,
    IReadOnlyList<string> Blockers,
    int LinksPlanned,
    int PaymentsPlanned,
    int NoChange)
{
    public bool HasWork => Blockers.Count == 0 && (LinksPlanned > 0 || PaymentsPlanned > 0);
}

/// <summary>One planned row after the sync: applied, or the refusal the handler gave.</summary>
public sealed record ValuationInvoicePaymentSyncResult(
    ValuationInvoicePaymentSyncRow Row,
    bool Applied,
    string? Error);

public sealed record ValuationInvoicePaymentSyncOutcome(
    string ProjectId,
    string ProjectName,
    DateTimeOffset? FetchedAtUtc,
    IReadOnlyList<ValuationInvoicePaymentSyncResult> Results,
    int Linked,
    int PaymentsRecorded,
    int NoChange,
    int Failed);
