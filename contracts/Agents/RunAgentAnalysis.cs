using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Agents;

// Ask an agent to analyse the request (all its emails, messages and files) and return a
// structured proposal for a human to review. A stub agent returns an Unavailable proposal.
public sealed record RunAgentAnalysis(string RequestId, string AgentKey) : ICommand<AgentProposal>;
