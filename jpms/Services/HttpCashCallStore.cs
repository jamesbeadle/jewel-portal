using Jewel.JPMS.Contracts.CashCalls;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpCashCallStore : ICashCallStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpCashCallStore(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    public event Action? OnChange;

    public Task<IReadOnlyList<CashCall>> ListAsync(string projectId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListCashCallsForProject(projectId), cancellationToken);

    public Task<ProjectCashCallSummary> GetSummaryAsync(string projectId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new GetProjectCashCallSummary(projectId), cancellationToken);

    public async Task<CashCall> CreateAsync(string projectId, DateTimeOffset periodMonth, decimal amountRequested, string? valuationClaimId, CancellationToken cancellationToken = default)
    {
        var created = await commands.SendAsync(new CreateCashCall(projectId, periodMonth, amountRequested, valuationClaimId), cancellationToken);
        OnChange?.Invoke();
        return created;
    }

    public async Task<CashCall> IssueInvoiceAsync(string cashCallId, CancellationToken cancellationToken = default)
    {
        var call = await commands.SendAsync(new IssueClientInvoice(cashCallId), cancellationToken);
        OnChange?.Invoke();
        return call;
    }

    public async Task<CashCall> RecordReceiptAsync(string cashCallId, decimal amountReceived, CancellationToken cancellationToken = default)
    {
        var call = await commands.SendAsync(new RecordCashCallReceipt(cashCallId, amountReceived), cancellationToken);
        OnChange?.Invoke();
        return call;
    }
}
