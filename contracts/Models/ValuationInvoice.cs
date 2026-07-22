namespace Jewel.JPMS.Models;

// Lifecycle of a valuation invoice (formerly "cash call"). The happy path is
// Raised (drafted against the current valuation; a report snapshot is frozen and attached) →
// Submitted (with the architect/client for approval) → Approved → Issued (client invoice sent — counts toward
// certified/invoiced to date) → Paid (client has paid — rolls into the project-level paid total).
// Submitted can come back Rejected, returning the invoice for amendment (→ Raised) or cancellation.
// Raised → Issued directly remains permitted for projects that skip the formal approval loop.
// Int values 0–2 predate the approval states and are persisted/seeded — never renumber.
public enum ValuationInvoiceStatus
{
    Raised = 0,
    Issued = 1,
    Paid = 2,
    Submitted = 3,
    Approved = 4,
    Rejected = 5,
    Cancelled = 6
}

// Everything that can happen to a valuation invoice, recorded as an audit trail so
// amendments are tracked on the same invoice (no versioning).
public enum ValuationInvoiceEventType
{
    Created = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Amended = 4,
    Issued = 5,
    PaymentRecorded = 6,
    Cancelled = 7,
    ManualEntry = 8
}

// A valuation invoice: the client invoice raised against the current valuation/CVR. Drawn from a
// valuation claim when one is linked. Issued/Paid invoices drive "Certified to date" on the
// valuation report; when paid, the amount rolls into the project-level total. Submitted/Approved
// invoices are pending — they never count toward certified until issued. Manual invoices are
// backdated historical entries created directly as Issued or Paid; they bypass the approval loop.
public sealed record ValuationInvoice(
    string ValuationInvoiceId,
    string ProjectId,
    string? ValuationClaimId,
    int Number,
    string Reference,             // e.g. "VI-0001"
    DateTimeOffset PeriodMonth,   // the month this invoice covers
    decimal Amount,
    decimal AmountPaid,
    ValuationInvoiceStatus Status,
    DateTimeOffset RaisedAt,
    DateTimeOffset? IssuedAt = null,
    DateTimeOffset? PaidAt = null,
    DateTimeOffset? SubmittedAt = null,
    DateTimeOffset? ApprovedAt = null,
    DateTimeOffset? RejectedAt = null,
    DateTimeOffset? CancelledAt = null,
    string? RejectionReason = null,
    int AmendmentCount = 0,
    bool IsManual = false,
    string? ValuationReportSnapshotId = null)  // latest snapshot backing this invoice
{
    public string DisplayNumber => Number > 0 ? $"VI-{Number:0000}" : "";

    // Pending states: claimed from the client but not yet certifiable.
    public bool IsAwaitingApproval => Status is ValuationInvoiceStatus.Submitted or ValuationInvoiceStatus.Approved;

    // Editable states: amount/period may still change (manual invoices stay editable throughout).
    public bool IsEditable => IsManual || Status is ValuationInvoiceStatus.Raised or ValuationInvoiceStatus.Rejected;
}

// One entry in a valuation invoice's audit trail.
public sealed record ValuationInvoiceEvent(
    string ValuationInvoiceEventId,
    string ValuationInvoiceId,
    ValuationInvoiceEventType EventType,
    DateTimeOffset OccurredAt,
    string Note,                  // e.g. rejection reason, amendment summary
    decimal? AmountBefore = null, // populated for Amended / PaymentRecorded
    decimal? AmountAfter = null);

// Project-level roll-up of valuation invoices. Cancelled invoices are excluded from every figure.
public sealed record ProjectValuationInvoiceSummary(
    string ProjectId,
    decimal TotalRaised,             // sum of all live invoice amounts, any status except Cancelled
    decimal TotalInvoiced,           // sum of Issued + Paid invoice amounts — feeds "Certified to date"
    decimal TotalPaid,               // sum of amounts the client has paid
    decimal Outstanding,             // invoiced but not yet paid
    decimal TotalAwaitingApproval = 0m); // sum of Submitted + Approved amounts — pending exposure
