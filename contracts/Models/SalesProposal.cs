namespace Jewel.JPMS.Models;

/// <summary>
/// A proposal (2026-09-06): the scoping-and-pricing stage after the concepts. The sales team
/// writes the scope, a base price, the options the prospect can add (each with its price
/// difference), a schedule of works (phases in weeks) and the terms; picks the concept render
/// that heads it; and sends it. The prospect opens it on the same private imagine page, toggles
/// the options and watches the price move, reads the timeline and the terms, and accepts — name,
/// email, the options they chose and the moment, recorded. Acceptance is the contract: the lead
/// page shows it and the directors mark the lead Won, which creates the client and the project.
/// Versions are kept: sending a new proposal supersedes the last.
/// </summary>
public enum SalesProposalStatus
{
    Draft = 0,
    Sent = 1,
    Accepted = 2,
    Declined = 3,
    Superseded = 4
}

public static class SalesProposalStatusExtensions
{
    public static string DisplayName(this SalesProposalStatus status) => status switch
    {
        SalesProposalStatus.Draft      => "Draft",
        SalesProposalStatus.Sent       => "Sent",
        SalesProposalStatus.Accepted   => "Accepted",
        SalesProposalStatus.Declined   => "Declined",
        SalesProposalStatus.Superseded => "Superseded",
        _ => status.ToString()
    };
}

/// <summary>An optional extra on a proposal, priced as a difference from the base.</summary>
public sealed record ProposalOption(
    string OptionId,
    string Name,
    string Description,
    decimal PriceDelta,
    // Pre-ticked when the proposal opens.
    bool Recommended);

/// <summary>One phase of the schedule of works: which week it starts and how long it runs.</summary>
public sealed record ProposalPhase(
    string Name,
    int StartWeek,
    int Weeks);

/// <summary>The full proposal as staff read and edit it.</summary>
public sealed record SalesProposal(
    string ProposalId,
    string LeadId,
    int Version,
    string Title,
    // Markdown: what is included, the specification, exclusions.
    string Scope,
    decimal BasePrice,
    IReadOnlyList<ProposalOption> Options,
    IReadOnlyList<ProposalPhase> Schedule,
    // Markdown: payment terms, validity, the contract terms accepted on acceptance.
    string Terms,
    // The concept render that heads the proposal, from the lead's imagine rounds.
    string? HeroImageId,
    SalesProposalStatus Status,
    string CreatedByEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? AcceptedAt,
    string? AcceptedByName,
    string? AcceptedByEmail,
    IReadOnlyList<string> AcceptedOptionIds,
    decimal? AcceptedPrice,
    DateTimeOffset? DeclinedAt,
    string? DeclineReason);

/// <summary>The proposal as the prospect sees it on the imagine page.</summary>
public sealed record ProposalView(
    string ProposalId,
    int Version,
    string Title,
    string Scope,
    decimal BasePrice,
    IReadOnlyList<ProposalOption> Options,
    IReadOnlyList<ProposalPhase> Schedule,
    string Terms,
    string? HeroImageId,
    SalesProposalStatus Status,
    DateTimeOffset? SentAt,
    DateTimeOffset? AcceptedAt,
    string? AcceptedByName,
    IReadOnlyList<string> AcceptedOptionIds,
    decimal? AcceptedPrice);

/// <summary>What the prospect submits to accept: who they are, the options they chose, and the
/// explicit agreement to the terms.</summary>
public sealed record ProposalAcceptance(
    string ProposalId,
    string Name,
    string Email,
    IReadOnlyList<string> OptionIds,
    bool AgreedToTerms);

/// <summary>A prospect turning a proposal down, with a word on why.</summary>
public sealed record ProposalDecline(string ProposalId, string Reason);
