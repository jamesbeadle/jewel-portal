using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Agents;

// The human-in-the-loop decision on a proposal: Accept applies it, otherwise it is rejected.
// DecidedByEmail is stamped server-side from the signed-in user.
public sealed record DecideAgentProposal(
    string ProposalId,
    bool Accept,
    string DecidedByEmail = "") : ICommand<AgentProposal>;
