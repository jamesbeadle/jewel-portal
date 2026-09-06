using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Sales;

/// <summary>
/// Issues (or re-issues) a lead's private imagine link — the token behind <c>/imagine/{token}</c>
/// and the QR code on the letter. Re-issuing replaces the token, so a QR code already printed
/// stops working: do it only when the old one must be cut off. Logged on the timeline.
/// IssuedByEmail is stamped by the server.
/// </summary>
public sealed record IssueImagineLink(string LeadId, string IssuedByEmail = "") : ICommand<Lead>;

/// <summary>Re-queues a round whose render failed (or never ran). Staff only.</summary>
public sealed record RetryImagineRound(string LeadId, string RoundId, string RequestedByEmail = "") : ICommand<ImagineRoundView>;

/// <summary>
/// Saves a proposal draft on a lead — a new version when ProposalId is blank, otherwise the named
/// draft (a sent proposal is never edited: send a new version instead). Options and phases are
/// the whole lists as they should stand. SavedByEmail is stamped by the server.
/// </summary>
public sealed record SaveSalesProposal(
    string LeadId,
    string? ProposalId,
    string Title,
    string Scope,
    decimal BasePrice,
    IReadOnlyList<ProposalOption> Options,
    IReadOnlyList<ProposalPhase> Schedule,
    string Terms,
    string? HeroImageId,
    string SavedByEmail = "") : ICommand<SalesProposal>;

/// <summary>
/// Sends a draft: it becomes the lead's live proposal (any earlier Sent one is Superseded), the
/// prospect is emailed the link to their imagine page (the proposal shows there), the lead moves
/// to Proposal and the timeline records it. A lead needs an imagine link and a contact email
/// first — the send refuses otherwise, and says so. SentByEmail is stamped by the server.
/// </summary>
public sealed record SendSalesProposal(string LeadId, string ProposalId, string? Note, string SentByEmail = "") : ICommand<SalesProposal>;

/// <summary>Withdraws a sent proposal (Superseded) without sending another. Directors only.</summary>
public sealed record WithdrawSalesProposal(string LeadId, string ProposalId, string DecidedByEmail = "") : ICommand<SalesProposal>;
