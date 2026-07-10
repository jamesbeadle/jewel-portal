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

    // Keys that have had a load started — prevents an empty result from
    // re-triggering a fetch on every re-render (see HttpDrawingStore).
    private readonly HashSet<string> requestsRequested = new();
    private bool queueRequested;

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
        if (requestsRequested.Add(requestId)) _ = LoadRequestAsync(requestId);
        return readModel.ForRequest(requestId);
    }

    private async Task LoadRequestAsync(string requestId)
    {
        try { await readModel.RefreshRequestAsync(requestId, CancellationToken.None); }
        catch { requestsRequested.Remove(requestId); }
    }

    public IReadOnlyList<AgentQueueItem> Queue()
    {
        if (!queueRequested) { queueRequested = true; _ = LoadQueueGuardedAsync(); }
        return readModel.Queue;
    }

    private async Task LoadQueueGuardedAsync()
    {
        try { await readModel.RefreshQueueAsync(CancellationToken.None); }
        catch { queueRequested = false; }
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

    public async Task<RequestCloseOutcome> AttemptCloseAsync(string requestId, DateTimeOffset? closedAt = null, CancellationToken cancellationToken = default)
    {
        var outcome = await commands.SendAsync(new AttemptCloseRequest(requestId, ClosedAt: closedAt), cancellationToken);
        await readModel.RefreshRequestAsync(requestId, cancellationToken);
        return outcome;
    }
}
