namespace Jewel.JPMS.Models;

public sealed record QualificationAssessment(
    string LeadId,
    int Score,
    string Notes,
    string AssessedByEmail,
    DateTimeOffset AssessedAt);

public sealed record SiteVisit(
    string SiteVisitId,
    string LeadId,
    DateTimeOffset ScheduledAt,
    IReadOnlyList<string> AttendeeEmails,
    string Notes,
    int PhotoCount,
    bool IsComplete);

public sealed record InfoChaseItem(
    string InfoChaseItemId,
    string LeadId,
    string Kind,
    string Description,
    bool IsReceived,
    DateTimeOffset RequestedAt);

public sealed record BidDecision(
    string LeadId,
    bool ShouldBid,
    string Reason,
    string DecidedByEmail,
    DateTimeOffset DecidedAt);

public sealed record NegotiationRound(
    DateTimeOffset At,
    decimal RevisedValue,
    string Notes);

public sealed record Proposal(
    string ProposalId,
    string LeadId,
    decimal Value,
    DateTimeOffset IssuedAt,
    IReadOnlyList<NegotiationRound> NegotiationRounds);

public sealed record LeadOutcome(
    string LeadId,
    bool IsWon,
    string Reason,
    string DecidedByEmail,
    DateTimeOffset DecidedAt,
    string? CreatedProjectId);
