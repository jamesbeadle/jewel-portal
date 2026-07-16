using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpVariationStore : IVariationStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpVariationStore(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    public event Action? OnChange;

    public Task<VariationOrderQuote?> GetByIdAsync(string voqId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new GetVoqById(voqId), cancellationToken);

    public Task<VariationOrderQuote?> GetByRequestAsync(string requestId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new GetVoqByRequest(requestId), cancellationToken);

    public Task<IReadOnlyList<VariationOrderQuote>> ListForProjectAsync(string projectId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListVoqsForProject(projectId), cancellationToken);

    public Task<IReadOnlyList<VariationOrder>> ListVariationOrdersForProjectAsync(string projectId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListVariationOrdersForProject(projectId), cancellationToken);

    public Task<IReadOnlyList<BidPackage>> ListBidPackagesAsync(string voqId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListBidPackagesForVoq(voqId), cancellationToken);

    public Task<VoqDraftProposal> PrepareVoqDraftAsync(string requestId, CancellationToken cancellationToken = default) =>
        // A command (POST) rather than a query: it spends an LLM call server-side. Nothing is saved.
        commands.SendAsync(new PrepareVoqDraft(requestId), cancellationToken);

    public async Task<VariationOrderQuote> CreateFromRfqAsync(string requestId, string? title = null, string? description = null, decimal? estimatedValue = null, CancellationToken cancellationToken = default)
    {
        // The API resolves the creator from the signed-in user, so the email here is a placeholder.
        var created = await commands.SendAsync(new CreateVoqFromRfq(requestId, string.Empty, title, description, estimatedValue), cancellationToken);
        OnChange?.Invoke();
        return created;
    }

    public async Task<BidPackage> AddBidPackageAsync(string voqId, string title, string trade, CancellationToken cancellationToken = default)
    {
        // OwnerEmail is set from the signed-in user server-side.
        var package = await commands.SendAsync(new AddBidPackageToVoq(voqId, title, trade, string.Empty), cancellationToken);
        OnChange?.Invoke();
        return package;
    }

    public async Task<VariationOrderQuote> SelectTenderAsync(string voqId, string bidPackageId, string subcontractorId, decimal? estimatedValue, CancellationToken cancellationToken = default)
    {
        var voq = await commands.SendAsync(new SelectVoqTender(voqId, bidPackageId, subcontractorId, estimatedValue), cancellationToken);
        OnChange?.Invoke();
        return voq;
    }

    public Task<VariationOrder?> GetVariationOrderByVoqAsync(string voqId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new GetVariationOrderByVoq(voqId), cancellationToken);

    public async Task<VariationOrder> ApproveVoqAsync(string voqId, string costCode, decimal? value, CancellationToken cancellationToken = default)
    {
        // ApprovedByEmail is set from the signed-in user server-side.
        var vo = await commands.SendAsync(new ApproveVariationOrderQuote(voqId, costCode, string.Empty, value), cancellationToken);
        OnChange?.Invoke();
        return vo;
    }

    public async Task<VariationOrder> IssueVariationOrderAsync(string voId, CancellationToken cancellationToken = default)
    {
        var vo = await commands.SendAsync(new IssueVariationOrder(voId), cancellationToken);
        OnChange?.Invoke();
        return vo;
    }

    public async Task<VariationOrder> CancelVariationOrderAsync(string voId, CancellationToken cancellationToken = default)
    {
        var vo = await commands.SendAsync(new CancelVariationOrder(voId), cancellationToken);
        OnChange?.Invoke();
        return vo;
    }

    public async Task<VariationOrder> ReviseVariationOrderValueAsync(string voId, decimal value, CancellationToken cancellationToken = default)
    {
        // RevisedByEmail is stamped from the signed-in user server-side.
        var vo = await commands.SendAsync(new ReviseVariationOrderValue(voId, value), cancellationToken);
        OnChange?.Invoke();
        return vo;
    }

    public Task<IReadOnlyList<SubcontractorVariationRequest>> ListVariationRequestsForProjectAsync(string projectId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListVariationRequestsForProject(projectId), cancellationToken);

    public async Task<VariationOrderQuote> AcceptVariationRequestAsync(string variationRequestId, CancellationToken cancellationToken = default)
    {
        // AcceptedByEmail is stamped from the signed-in user server-side.
        var voq = await commands.SendAsync(new AcceptVariationRequest(variationRequestId), cancellationToken);
        OnChange?.Invoke();
        return voq;
    }

    public async Task<SubcontractorVariationRequest> RejectVariationRequestAsync(string variationRequestId, string reason, CancellationToken cancellationToken = default)
    {
        var rejected = await commands.SendAsync(new RejectVariationRequest(variationRequestId, reason), cancellationToken);
        OnChange?.Invoke();
        return rejected;
    }

    public async Task<WorkOrder> IssueWorkOrderForVariationOrderAsync(string variationOrderId, CancellationToken cancellationToken = default)
    {
        // IssuedByEmail is stamped from the signed-in user server-side.
        var workOrder = await commands.SendAsync(new Jewel.JPMS.Contracts.Procurement.IssueWorkOrderForVariationOrder(variationOrderId), cancellationToken);
        OnChange?.Invoke();
        return workOrder;
    }
}
