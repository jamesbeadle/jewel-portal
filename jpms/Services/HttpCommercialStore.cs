using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Cashflow;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed partial class HttpCommercialStore : ICommercialStore
{
    private static readonly CashflowSnapshot NoCashflowYet = new("", DateTimeOffset.MinValue, 0m, 0m, 0m);

    private readonly ValuationsReadModel valuationsReadModel;
    private readonly CostCodeBudgetsReadModel budgetsReadModel;
    private readonly TimesheetsReadModel timesheetsReadModel;
    private readonly CashflowReadModel cashflowReadModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    // Projects whose data has had a load started — prevents an empty result
    // from re-triggering a fetch on every re-render (see HttpDrawingStore).
    private readonly HashSet<string> valuationsRequested = new();
    private readonly HashSet<string> budgetsRequested = new();
    private readonly HashSet<string> timesheetsRequested = new();

    public HttpCommercialStore(ValuationsReadModel valuationsReadModel, CostCodeBudgetsReadModel budgetsReadModel, TimesheetsReadModel timesheetsReadModel, CashflowReadModel cashflowReadModel, IQueryClient queries, ICommandSender commands)
    {
        this.valuationsReadModel = valuationsReadModel;
        this.budgetsReadModel = budgetsReadModel;
        this.timesheetsReadModel = timesheetsReadModel;
        this.cashflowReadModel = cashflowReadModel;
        this.queries = queries;
        this.commands = commands;
        valuationsReadModel.OnChanged += () => OnChange?.Invoke();
        budgetsReadModel.OnChanged += () => OnChange?.Invoke();
        timesheetsReadModel.OnChanged += () => OnChange?.Invoke();
        cashflowReadModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<ClaimPeriod> ClaimPeriodsFor(string projectId) =>
        queries.AskAsync(new ListClaimPeriodsForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public IReadOnlyList<Valuation> ValuationsFor(string projectId)
    {
        if (valuationsRequested.Add(projectId)) _ = LoadValuationsAsync(projectId);
        return valuationsReadModel.Current(projectId);
    }

    private async Task LoadValuationsAsync(string projectId)
    {
        try { await valuationsReadModel.RefreshAsync(projectId, CancellationToken.None); }
        catch { valuationsRequested.Remove(projectId); }
    }

    public Valuation SaveValuation(Valuation valuation)
    {
        if (string.IsNullOrEmpty(valuation.ValuationId))
            _ = DraftAsync(valuation);
        else if (valuation.IsIssued)
            _ = IssueAsync(valuation);
        else
            _ = ReviseAsync(valuation);
        return valuation;
    }

    public CashflowSnapshot LatestCashflow()
    {
        if (!cashflowReadModel.HasLoaded) _ = cashflowReadModel.RefreshAsync(CancellationToken.None);
        return cashflowReadModel.Current ?? NoCashflowYet;
    }

    private async Task DraftAsync(Valuation valuation)
    {
        await commands.SendAsync(new DraftValuation(valuation.ProjectId, valuation.ClaimPeriodId, valuation.GrossValue, valuation.RetentionPercent), CancellationToken.None);
        await valuationsReadModel.RefreshAsync(valuation.ProjectId, CancellationToken.None);
    }

    private async Task IssueAsync(Valuation valuation)
    {
        await commands.SendAsync(new IssueValuation(valuation.ValuationId), CancellationToken.None);
        await valuationsReadModel.RefreshAsync(valuation.ProjectId, CancellationToken.None);
    }

    private async Task ReviseAsync(Valuation valuation)
    {
        await commands.SendAsync(new ReviseValuation(valuation.ValuationId, valuation.GrossValue, valuation.RetentionPercent), CancellationToken.None);
        await valuationsReadModel.RefreshAsync(valuation.ProjectId, CancellationToken.None);
    }
}
