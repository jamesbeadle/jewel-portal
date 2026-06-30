using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Agents;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpAgentDesk : IAgentDesk
{
    private readonly AgentsReadModel readModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpAgentDesk(AgentsReadModel readModel, IQueryClient queries, ICommandSender commands)
    {
        this.readModel = readModel;
        this.queries = queries;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<RequestAgent> ForRequest(string requestId)
    {
        if (readModel.ForRequest(requestId).Count == 0) _ = readModel.RefreshRequestAsync(requestId, CancellationToken.None);
        return readModel.ForRequest(requestId);
    }

    public IReadOnlyList<AgentQueueItem> Queue()
    {
        if (readModel.Queue.Count == 0) _ = readModel.RefreshQueueAsync(CancellationToken.None);
        return readModel.Queue;
    }

    public async Task<IReadOnlyList<RequestAgent>> LoadRequestAgentsAsync(string requestId, CancellationToken cancellationToken = default)
    {
        await readModel.RefreshRequestAsync(requestId, cancellationToken);
        return readModel.ForRequest(requestId);
    }

    public async Task<IReadOnlyList<AgentQueueItem>> LoadQueueAsync(CancellationToken cancellationToken = default)
    {
        await readModel.RefreshQueueAsync(cancellationToken);
        return readModel.Queue;
    }

    public Task<IReadOnlyList<AgentChatMessage>> ListChatAsync(string requestId, string agentKey, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListAgentChat(requestId, agentKey), cancellationToken);

    public Task<AgentChatMessage> SendMessageAsync(string requestId, string agentKey, string body, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new SendAgentMessage(requestId, agentKey, body), cancellationToken);

    public async Task<AgentProposal> RunAnalysisAsync(string requestId, string agentKey, CancellationToken cancellationToken = default)
    {
        var proposal = await commands.SendAsync(new RunAgentAnalysis(requestId, agentKey), cancellationToken);
        await readModel.RefreshRequestAsync(requestId, cancellationToken);
        return proposal;
    }

    public Task<IReadOnlyList<AgentProposal>> ListProposalsAsync(string requestId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListAgentProposals(requestId), cancellationToken);

    public Task<AgentProposal> DecideAsync(string proposalId, bool accept, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new DecideAgentProposal(proposalId, accept), cancellationToken);

    public async Task<RequestCloseOutcome> AttemptCloseAsync(string requestId, CancellationToken cancellationToken = default)
    {
        var outcome = await commands.SendAsync(new AttemptCloseRequest(requestId), cancellationToken);
        await readModel.RefreshRequestAsync(requestId, cancellationToken);
        return outcome;
    }
}
