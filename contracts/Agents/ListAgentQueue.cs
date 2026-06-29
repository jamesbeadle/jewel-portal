using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Agents;

// The global queue: every watched (request, agent) pair across the portfolio.
public sealed record ListAgentQueue : IQuery<IReadOnlyList<AgentQueueItem>>;
