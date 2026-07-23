namespace Jewel.JPMS.Models;

// Lifecycle of a Variation Order (VO) — ONE document from first pricing to client decision.
// A "VOQ" was never a separate thing: it was this document in its quoting stage, and the
// 2026-07-23 unification folded the two records into one (see CLAUDE.md terminology).
//
//   Quoting  — draft: gathering information (bid packages out to subcontractors, pricing,
//              correspondence). No commercial effect.
//   Issued   — sent to the client for a decision; not yet approved or rejected. Still no
//              commercial effect.
//   Approved — the client's instruction to proceed. Approval mints the V-ref and writes the
//              value through to the Valuation Report, the CVR and the cost-centre budget.
//   Rejected — declined by the client, or withdrawn. Rejecting an APPROVED variation is a real
//              commercial event: it reverses the approval's valuation/CVR/budget writes.
public enum VariationOrderStatus
{
    Quoting = 0,
    Issued = 1,
    Approved = 2,
    Rejected = 3
}

// The unified Variation Order. Exists from the moment an RFQ is priced up; carries its quoting
// data (bid packages link to it, the selected tender and estimate live on it) and, once approved,
// its contract data (V-ref, agreed value, cost code). References keep the historic "VOQ-0001"
// spelling because they are threaded into mail tags (JPMS/VOQ-…) — an identifier, not UI copy.
public sealed record VariationOrder(
    string VariationOrderId,
    string ProjectId,
    string RequestId,               // the RFQ (request) it was created from
    int Number,                     // sequential per project; rendered VOQ-0001
    string Reference,               // human reference, e.g. "VOQ-0001" (historic prefix, kept for mail-tag continuity)
    string Title,
    string Description,
    VariationOrderStatus Status,
    string? SelectedBidPackageId,   // winning tender, when one was selected during Quoting
    string? SelectedSubcontractorId,// who is doing the work (from the selected tender)
    decimal? EstimatedValue,        // the quoting-stage estimate; Value is the approved figure
    string? VariationRef,           // e.g. "V18" — minted at approval; null until then
    decimal Value,                  // approved (contract) value; 0 until approved
    string? CostCode,               // budget category the value is committed against at approval
    DateTimeOffset CreatedAt,
    string CreatedByEmail,
    DateTimeOffset? IssuedAt = null,
    DateTimeOffset? ApprovedAt = null,
    string? ApprovedByEmail = null,
    DateTimeOffset? RejectedAt = null)
{
    public string DisplayNumber => Number > 0 ? $"VOQ-{Number:0000}" : "";
}
