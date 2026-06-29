using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Agents;

// All proposals produced by agents on a request (newest first) for the review screen.
public sealed record ListAgentProposals(string RequestId) : IQuery<IReadOnlyList<AgentProposal>>;
