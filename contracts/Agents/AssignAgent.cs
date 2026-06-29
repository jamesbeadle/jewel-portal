using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Agents;

// Apply an agent to a request: adds the watch-queue row and (if IsPrimary) marks it the
// request's lead agent. AssignedByEmail is stamped server-side from the signed-in user.
public sealed record AssignAgent(
    string RequestId,
    string AgentKey,
    bool IsPrimary = false,
    string AssignedByEmail = "") : ICommand<RequestAgent>;
