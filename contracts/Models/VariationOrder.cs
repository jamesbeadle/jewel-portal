namespace Jewel.JPMS.Models;

// Lifecycle of a Variation Order (VO) — the priced, approved change to the contract raised from an
// approved VOQ. Approved on creation; Issued once formally instructed; Cancelled if withdrawn.
public enum VariationOrderStatus
{
    Approved = 0,
    Issued = 1,
    Cancelled = 2
}

// A Variation Order: the first-class, approved change. Raised when a VOQ is approved. On creation it
// writes its value into the Valuation Report (as a Variation line), the CVR (a QS accrual) and the
// cost-centre budget (committed amount) — the diagram's "Add VO to CVR".
public sealed record VariationOrder(
    string VariationOrderId,
    string ProjectId,
    string VariationOrderQuoteId,   // the VOQ it was approved from
    string RequestId,               // provenance back to the originating request
    int Number,                     // sequential; rendered V01, V02, …
    string VariationRef,            // e.g. "V18" — reused as ValuationLineItem.VariationRef
    string Title,
    string Description,
    VariationOrderStatus Status,
    decimal Value,                  // net value of the variation
    string? SubcontractorId,        // who is doing the work (from the selected tender)
    string CostCode,                // budget category the value is committed against
    DateTimeOffset ApprovedAt,
    string ApprovedByEmail,
    DateTimeOffset? IssuedAt = null,
    DateTimeOffset? CancelledAt = null);
