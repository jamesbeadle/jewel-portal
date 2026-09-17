using Jewel.JPMS.Contracts.Sales;
using Jewel.JPMS.Features.Sales;

namespace Jewel.JPMS.Pages;

public partial class SalesStrategyDetail
{
    [Parameter] public string StrategyId { get; set; } = "";

    private bool sessionReady;
    private bool dataFailed;
    private string loadedForId = "";

    private SalesStrategy? Strategy => Detail.Current(StrategyId)?.Strategy;
    private SalesStrategyFunnel Funnel => Detail.Current(StrategyId)?.Funnel ?? SalesStrategyFunnel.Empty;
    private IReadOnlyList<Lead> Leads => Detail.Current(StrategyId)?.Leads ?? Array.Empty<Lead>();

    // The lead form's picker needs the overview list; this page has the one strategy, so offer
    // the list when it has loaded and fall back to just this strategy.
    private IReadOnlyList<SalesStrategyOverview> StrategyOptionsList =>
        Strategies.Current ?? (Strategy is { } s ? new[] { new SalesStrategyOverview(s, Funnel) } : Array.Empty<SalesStrategyOverview>());

    protected override async Task OnInitializedAsync()
    {
        Detail.OnChanged += StateHasChanged;
        Strategies.OnChanged += StateHasChanged;
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        sessionReady = true;
        await LoadAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!sessionReady || loadedForId == StrategyId) return;
        dataFailed = false;
        actionError = null;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        loadedForId = StrategyId;
        try
        {
            await Task.WhenAll(
                Detail.RefreshAsync(StrategyId, CancellationToken.None),
                Strategies.Current is null ? Strategies.RefreshAsync(CancellationToken.None) : Task.CompletedTask);
        }
        catch { dataFailed = true; }
        EnsurePolling();
    }

    private async Task ReloadAsync()
    {
        try { await Detail.RefreshAsync(StrategyId, CancellationToken.None); }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    public void Dispose()
    {
        polling?.Cancel();
        Detail.OnChanged -= StateHasChanged;
        Strategies.OnChanged -= StateHasChanged;
    }
}
