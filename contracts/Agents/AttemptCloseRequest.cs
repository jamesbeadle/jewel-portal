using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Agents;

// Try to close a request through the agent gate. Every applied agent must agree its work is
// complete; if any disagrees the request stays open and the outcome lists the blockers.
public sealed record AttemptCloseRequest(string RequestId, string ClosedByEmail = "") : ICommand<RequestCloseOutcome>;
