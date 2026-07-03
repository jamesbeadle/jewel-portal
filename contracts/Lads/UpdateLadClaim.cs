using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Lads;

// Update a recorded LADs claim — commercial details and status as the claim moves through
// Notified → Disputed / Agreed / Withdrawn / Settled.
public sealed record UpdateLadClaim(
    string LadClaimId,
    string Title,
    string? Description,
    DateTimeOffset? PeriodFrom,
    DateTimeOffset? PeriodTo,
    int DaysClaimed,
    decimal RatePerWeek,
    decimal Amount,
    LadStatus Status,
    DateTimeOffset? RaisedAt = null) : ICommand<LadClaim>;
