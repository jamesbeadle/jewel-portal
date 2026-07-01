namespace Jewel.JPMS.Models;

// Lifecycle of a Variation Order Quote (VOQ). Created from an RFQ, it gathers bid packages sent to
// subcontractors; once a winning tender is selected and it is approved, a Variation Order is raised.
public enum VariationOrderQuoteStatus
{
    Draft = 0,      // created, no bid packages yet
    Inviting = 1,   // one or more bid packages exist and are out to tender
    Tendering = 2,  // quotes are coming back
    Selected = 3,   // a winning tender has been chosen
    Approved = 4,   // approved -> a Variation Order has been raised (Phase 3)
    Rejected = 5
}

// A Variation Order Quote: the procurement container an RFQ creates. It holds an array of bid
// packages (each sent to subcontractors from the directory), records the selected subcontractor and
// the budget, and, when approved, produces a Variation Order.
public sealed record VariationOrderQuote(
    string VariationOrderQuoteId,
    string ProjectId,
    string RequestId,              // the RFQ (request) it was created from
    int Number,                   // sequential; rendered VOQ-0001
    string Reference,             // human reference, e.g. "VOQ-0001"
    string Title,
    string Description,
    VariationOrderQuoteStatus Status,
    string? SelectedBidPackageId,
    string? SelectedSubcontractorId,
    decimal? EstimatedValue,
    DateTimeOffset CreatedAt,
    string CreatedByEmail,
    DateTimeOffset? ApprovedAt = null,
    string? ApprovedByEmail = null)
{
    public string DisplayNumber => Number > 0 ? $"VOQ-{Number:0000}" : "";
}
