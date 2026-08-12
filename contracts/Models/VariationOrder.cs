namespace Jewel.JPMS.Models;

// Lifecycle of a Variation Order (VO) — ONE document from first pricing to client decision.
// A "VOQ" was never a separate thing: it was this document in its quoting stage, and the
// 2026-07-23 unification folded the two records into one (see CLAUDE.md terminology).
//
//   Quoting  — draft: gathering information (bid packages out to subcontractors, pricing,
//              correspondence). No commercial effect.
//   Issued   — sent to the client for a decision; not yet approved or rejected. Still no
//              commercial effect.
//   Awaiting AI — issued, and now waiting on a formal Architect's Instruction before the client
//              decides; still a pre-approval, side-effect-free stage (UI label "Awaiting AI").
//   Approved — the client's instruction to proceed. Approval mints the V-ref and writes the
//              value through to the Valuation Report, the CVR and the cost-centre budget.
//   Rejected — declined by the client, or withdrawn. Rejecting an APPROVED variation is a real
//              commercial event: it reverses the approval's valuation/CVR/budget writes.
public enum VariationOrderStatus
{
    Quoting = 0,
    Issued = 1,
    Approved = 2,
    Rejected = 3,
    // Client has been issued the variation and now awaits a formal Architect's Instruction before
    // deciding — a side-effect-free waiting stage between Issued and Approved. Appended as 4;
    // never renumber (persisted as an int on the row — see VariationStatusTests). UI: "Awaiting AI".
    AwaitingArchitectInstruction = 4
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
    string? SelectedBidPackageId,   // LEGACY (pre-2026-08-12): the winning package when tenders ran
                                    // through the VO. No longer written — kept for old records
    string? SelectedSubcontractorId,// who the works will be instructed to (recorded agreed tender)
    decimal? EstimatedValue,        // the quoting-stage estimate; Value is the approved figure
    string? VariationRef,           // e.g. "V18" — minted at approval; null until then
    decimal Value,                  // approved (contract) value; 0 until approved
    string? CostCode,               // budget category the value is committed against at approval
    DateTimeOffset CreatedAt,
    string CreatedByEmail,
    DateTimeOffset? IssuedAt = null,
    DateTimeOffset? ApprovedAt = null,
    string? ApprovedByEmail = null,
    DateTimeOffset? RejectedAt = null,
    // ---- Narrative sections of the issued VO document (all optional free text) ----------------
    // Printed on the official PDF between the header and the cost breakdown / at its foot, in the
    // order the client reads them: what the price is based on, what it does to the programme, and
    // what it deliberately leaves out. Editable at every stage — wording is refined right up to
    // (and after) issue, the same rule as the title.
    string? CommercialBasis = null,   // pricing basis: tender-face position, rate basis, OH&P…
    string? ProgrammeImpact = null,   // effect on the programme (procurement, mobilisation, duration)
    string? Exclusions = null)        // what this VO expressly does not price
{
    /// <summary>What a user sees this variation called, at every stage: "V72".
    ///
    /// There is one number for one document. Approval mints <see cref="VariationRef"/> from this
    /// same number (VOQ-0072 → V72), so showing "VOQ-0072" while quoting and "V72" once approved
    /// only ever made one record look like two. <see cref="Reference"/> keeps its historic
    /// "VOQ-0072" spelling because it is a persisted identifier; the mail-tag stem is built
    /// separately from <see cref="Number"/> (see VariationOrderQuoteLinkProvider), so what is shown
    /// here is free to differ from either without touching mailbox triage.</summary>
    public string DisplayNumber => Number > 0 ? $"V{Number}" : "";
}

public static class VariationOrderStatusExtensions
{
    // The one shared status wording. Because a variation is a single document, its status is the
    // only thing telling a reader which stage it has reached — so every pill, lineage chip, picker
    // and export goes through here and they can never drift apart. (Same arrangement as
    // RequestStatusExtensions.)
    public static string DisplayName(this VariationOrderStatus status) => status switch
    {
        VariationOrderStatus.Quoting  => "Quoting",
        VariationOrderStatus.Issued   => "Issued",
        VariationOrderStatus.AwaitingArchitectInstruction => "Awaiting AI",
        VariationOrderStatus.Approved => "Approved",
        VariationOrderStatus.Rejected => "Rejected",
        _ => status.ToString()
    };

    /// <summary>The tooltip/hint wording that accompanies the label wherever a surface shows one.</summary>
    public static string Hint(this VariationOrderStatus status) => status switch
    {
        VariationOrderStatus.Quoting  => "being priced — bid packages out, no commercial effect yet",
        VariationOrderStatus.Issued   => "with the client for a decision — no commercial effect yet",
        VariationOrderStatus.AwaitingArchitectInstruction => "issued, awaiting a formal Architect's Instruction",
        VariationOrderStatus.Approved => "instructed by the client — the value is in the valuation, CVR and budget",
        VariationOrderStatus.Rejected => "declined or withdrawn",
        _ => status.ToString()
    };

    /// <summary>True while the variation is still pre-approval, so it carries no commercial effect
    /// and can move freely between stages.</summary>
    public static bool IsPreApproval(this VariationOrderStatus status) =>
        status is VariationOrderStatus.Quoting
               or VariationOrderStatus.Issued
               or VariationOrderStatus.AwaitingArchitectInstruction;
}
