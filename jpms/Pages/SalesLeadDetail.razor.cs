using Jewel.JPMS.Contracts.Sales;
using Jewel.JPMS.Features.Sales;

namespace Jewel.JPMS.Pages;

public partial class SalesLeadDetail
{
    [Parameter] public string LeadId { get; set; } = "";

    private bool sessionReady;
    private bool dataFailed;
    private string loadedForId = "";

    private LeadDetail? Current => Detail.Current(LeadId);
    private Lead? Lead => Current?.Lead;
    private IReadOnlyList<LeadActivity> Activities => Current?.Activities ?? Array.Empty<LeadActivity>();
    private IReadOnlyList<SalesProposal> Proposals => Current?.Proposals ?? Array.Empty<SalesProposal>();
    private IReadOnlyList<LeadEstimate> Estimates => Current?.Estimates ?? Array.Empty<LeadEstimate>();
    private IReadOnlyList<ImagineImageView> Concepts =>
        Current?.Imagine?.Rounds.SelectMany(round => round.Concepts).ToList() ?? new List<ImagineImageView>();

    private bool CanWork => SalesAccess.CanWork(Session.ActiveRole);
    private bool CanDecide => SalesAccess.CanDecide(Session.ActiveRole);

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
        if (!sessionReady || loadedForId == LeadId) return;
        dataFailed = false;
        actionNote = null;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        loadedForId = LeadId;
        try
        {
            await Task.WhenAll(
                Detail.RefreshAsync(LeadId, CancellationToken.None),
                Strategies.Current is null ? Strategies.RefreshAsync(CancellationToken.None) : Task.CompletedTask);
        }
        catch { dataFailed = true; }
    }

    private async Task ReloadAsync()
    {
        // Post-write reload: swallow query failures (the toast already reported them).
        try { await Detail.RefreshAsync(LeadId, CancellationToken.None); }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    public void Dispose()
    {
        Detail.OnChanged -= StateHasChanged;
        Strategies.OnChanged -= StateHasChanged;
    }
}
