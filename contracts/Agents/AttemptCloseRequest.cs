using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Agents;

// Try to close a request through the agent gate. Every applied agent must agree its work is
// complete; if any disagrees the request stays open and the outcome lists the blockers.
// ClosedAt is the date the request is recorded as closed — chosen by the user (defaults to today,
// may be a prior date when the closure is only recorded later; never in the future). Null falls
// back to the moment the close is processed.
public sealed record AttemptCloseRequest(string RequestId, string ClosedByEmail = "", DateTimeOffset? ClosedAt = null) : ICommand<RequestCloseOutcome>;
