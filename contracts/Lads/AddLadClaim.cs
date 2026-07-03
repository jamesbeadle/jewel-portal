using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Lads;

// Record a Liquidated Damages claim the client has notified against the project. CreatedByEmail is
// stamped from the signed-in user server-side — never trusted from the client body. RaisedAt is the
// date of the client's notice; left null it defaults to "now".
public sealed record AddLadClaim(
    string ProjectId,
    string Title,
    string? Description = null,
    DateTimeOffset? PeriodFrom = null,
    DateTimeOffset? PeriodTo = null,
    int DaysClaimed = 0,
    decimal RatePerWeek = 0m,
    decimal Amount = 0m,
    DateTimeOffset? RaisedAt = null,
    string CreatedByEmail = "") : ICommand<LadClaim>;
