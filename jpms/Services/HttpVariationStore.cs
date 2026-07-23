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

    public Task<VariationOrder?> GetByIdAsync(string variationOrderId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new GetVariationOrderById(variationOrderId), cancellationToken);

    public Task<VariationOrder?> GetByRequestAsync(string requestId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new GetVoqByRequest(requestId), cancellationToken);

    public Task<IReadOnlyList<VariationOrder>> ListForProjectAsync(string projectId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListVariationOrdersForProject(projectId), cancellationToken);

    public Task<IReadOnlyList<BidPackage>> ListBidPackagesAsync(string variationOrderId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListBidPackagesForVoq(variationOrderId), cancellationToken);

    public Task<VoqDraftProposal> PrepareVoqDraftAsync(string requestId, CancellationToken cancellationToken = default) =>
        // A command (POST) rather than a query: it spends an LLM call server-side. Nothing is saved.
        commands.SendAsync(new PrepareVoqDraft(requestId), cancellationToken);

    public async Task<VariationOrder> CreateFromRfqAsync(string requestId, string? title = null, string? description = null, decimal? estimatedValue = null, CancellationToken cancellationToken = default)
    {
        // The API resolves the creator from the signed-in user, so the email here is a placeholder.
        var created = await commands.SendAsync(new CreateVoqFromRfq(requestId, string.Empty, title, description, estimatedValue), cancellationToken);
        OnChange?.Invoke();
        return created;
    }

    public async Task<BidPackage> AddBidPackageAsync(string variationOrderId, string title, string trade, CancellationToken cancellationToken = default)
    {
        // OwnerEmail is set from the signed-in user server-side.
        var package = await commands.SendAsync(new AddBidPackageToVoq(variationOrderId, title, trade, string.Empty), cancellationToken);
        OnChange?.Invoke();
        return package;
    }

    public async Task<VariationOrder> SelectTenderAsync(string variationOrderId, string bidPackageId, string subcontractorId, decimal? estimatedValue, CancellationToken cancellationToken = default)
    {
        var order = await commands.SendAsync(new SelectVoqTender(variationOrderId, bidPackageId, subcontractorId, estimatedValue), cancellationToken);
        OnChange?.Invoke();
        return order;
    }

    public async Task<VariationOrder> LinkToRequestAsync(string variationOrderId, string requestId, CancellationToken cancellationToken = default)
    {
        var order = await commands.SendAsync(new LinkVoqToRequest(variationOrderId, requestId), cancellationToken);
        OnChange?.Invoke();
        return order;
    }

    public async Task<VariationOrder> ApproveAsync(string variationOrderId, string costCode, decimal? value, CancellationToken cancellationToken = default)
    {
        // ApprovedByEmail is set from the signed-in user server-side.
        var order = await commands.SendAsync(new ApproveVariationOrder(variationOrderId, costCode, string.Empty, value), cancellationToken);
        OnChange?.Invoke();
        return order;
    }

    public async Task<VariationOrder> ReturnToQuotingAsync(string variationOrderId, CancellationToken cancellationToken = default)
    {
        var order = await commands.SendAsync(new ReturnVariationOrderToQuoting(variationOrderId), cancellationToken);
        OnChange?.Invoke();
        return order;
    }

    public async Task<VariationOrder> RejectAsync(string variationOrderId, CancellationToken cancellationToken = default)
    {
        var order = await commands.SendAsync(new RejectVariationOrder(variationOrderId), cancellationToken);
        OnChange?.Invoke();
        return order;
    }

    public async Task<VariationOrder> SetStatusAsync(string variationOrderId, VariationOrderStatus status, CancellationToken cancellationToken = default)
    {
        var order = await commands.SendAsync(new SetVariationOrderStatus(variationOrderId, status), cancellationToken);
        OnChange?.Invoke();
        return order;
    }

    public async Task<VariationOrder> ReviseVariationOrderValueAsync(string variationOrderId, decimal value, CancellationToken cancellationToken = default)
    {
        // RevisedByEmail is stamped from the signed-in user server-side.
        var order = await commands.SendAsync(new ReviseVariationOrderValue(variationOrderId, value), cancellationToken);
        OnChange?.Invoke();
        return order;
    }

    public Task<IReadOnlyList<SubcontractorVariationRequest>> ListVariationRequestsForProjectAsync(string projectId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListVariationRequestsForProject(projectId), cancellationToken);

    public async Task<VariationOrder> AcceptVariationRequestAsync(string variationRequestId, CancellationToken cancellationToken = default)
    {
        // AcceptedByEmail is stamped from the signed-in user server-side.
        var order = await commands.SendAsync(new AcceptVariationRequest(variationRequestId), cancellationToken);
        OnChange?.Invoke();
        return order;
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
