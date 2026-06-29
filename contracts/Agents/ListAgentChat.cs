using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Agents;

// The conversation for one agent on one request (the chat behind the agent dropdown).
public sealed record ListAgentChat(string RequestId, string AgentKey) : IQuery<IReadOnlyList<AgentChatMessage>>;
