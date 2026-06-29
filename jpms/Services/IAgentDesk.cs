using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IAgentDesk
{
    event Action? OnChange;

    IReadOnlyList<AgentDescriptor> Available();
    IReadOnlyList<RequestAgent> ForRequest(string requestId);
    IReadOnlyList<AgentQueueItem> Queue();

    Task<IReadOnlyList<AgentDescriptor>> LoadAvailableAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RequestAgent>> LoadRequestAgentsAsync(string requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgentQueueItem>> LoadQueueAsync(CancellationToken cancellationToken = default);

    Task<RequestAgent> AssignAsync(string requestId, string agentKey, bool isPrimary = false, CancellationToken cancellationToken = default);
    Task RemoveAsync(string requestId, string requestAgentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentChatMessage>> ListChatAsync(string requestId, string agentKey, CancellationToken cancellationToken = default);
    Task<AgentChatMessage> SendMessageAsync(string requestId, string agentKey, string body, CancellationToken cancellationToken = default);

    Task<AgentProposal> RunAnalysisAsync(string requestId, string agentKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgentProposal>> ListProposalsAsync(string requestId, CancellationToken cancellationToken = default);
    Task<AgentProposal> DecideAsync(string proposalId, bool accept, CancellationToken cancellationToken = default);

    Task<RequestCloseOutcome> AttemptCloseAsync(string requestId, CancellationToken cancellationToken = default);
}
