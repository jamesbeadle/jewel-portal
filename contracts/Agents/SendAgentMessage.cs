using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Agents;

// Post a message to an agent and get its reply appended to the thread. AuthorEmail/AuthorName
// are stamped server-side from the signed-in user. A stub agent replies "not implemented".
public sealed record SendAgentMessage(
    string RequestId,
    string AgentKey,
    string Body,
    string AuthorEmail = "",
    string AuthorName = "") : ICommand<AgentChatMessage>;
