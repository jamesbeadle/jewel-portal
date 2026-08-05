using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Procurement;

// Ask Claude to draft this bid package — a scope note and proposed line items — from what's tagged
// to it: the related emails and the linked drawings' register entries, plus the package's title and
// trade. This is a PROPOSAL — nothing is saved. The user reviews/edits the lines in the UI and only
// the ones they accept are committed with AddBidPackageLineItems (append-only, so existing content
// is never changed or removed). Degrades gracefully: when no LLM is configured, or the model's
// answer can't be parsed, Proposed is false and the UI explains rather than erroring.
public sealed record GenerateBidPackageDraft(
    string BidPackageId) : ICommand<BidPackageDraftProposal>;

// What Claude proposed. Notes carries the drafted scope summary and any assumptions the model made
// (shown above the lines in the review screen). CostCode is a code from the cost-centre master
// list, or "" when the model couldn't place the line — the user picks in review; every accepted
// line must have one before it can be applied.
public sealed record BidPackageDraftProposal(
    bool Proposed,                       // true when the LLM produced this; false = not configured / no answer
    string Notes,
    IReadOnlyList<BidPackageDraftLine> Lines);

public sealed record BidPackageDraftLine(
    string Trade,
    string Description,
    string Unit,
    decimal Quantity,
    string CostCode);
