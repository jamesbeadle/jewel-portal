using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// One suggested scope-of-works line for the VOQ's first bid package. Mirrors
/// BidPackageLineItemInput so an accepted line maps 1:1 onto the procurement line-item commands.
/// </summary>
public sealed record VoqDraftLine(
    string Trade,
    string Description,
    string Unit,
    decimal Quantity);

/// <summary>
/// A proposed VOQ drafted by the LLM from the RFI and the emails tagged to it. Nothing is saved —
/// the proposal goes to the UI for human review; CreateVoqFromRfq commits what the user accepts.
/// Proposed = false means the draft degraded to a skeleton (the RFI's own title/description
/// verbatim, no value, no lines) because no LLM is configured or its answer couldn't be used.
/// </summary>
public sealed record VoqDraftProposal(
    bool Proposed,
    string Title,
    string Description,
    decimal? EstimatedValue,
    string SuggestedTrade,
    IReadOnlyList<VoqDraftLine> Lines);

/// <summary>
/// Drafts a VOQ from a request that has an RFQ enabled, feeding the request's header, official
/// document and full tagged-email conversation to the LLM.
/// </summary>
public sealed record PrepareVoqDraft(string RequestId) : ICommand<VoqDraftProposal>;
