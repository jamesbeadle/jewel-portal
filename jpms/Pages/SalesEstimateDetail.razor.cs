using Jewel.JPMS.Contracts.Sales;
using Jewel.JPMS.Features.Sales;

namespace Jewel.JPMS.Pages;

public partial class SalesEstimateDetail
{
    [Parameter] public string LeadId { get; set; } = "";
    [Parameter] public string EstimateId { get; set; } = "";

    private bool sessionReady;
    private bool dataFailed;
    private string loadedForId = "";

    private EstimateBreakdownDraft breakdown = new();
    private int seeding;

    private Lead? Lead => Detail.Current(LeadId)?.Lead;
    private LeadEstimate? Estimate => Detail.Current(LeadId)?.Estimates?.FirstOrDefault(e => e.EstimateId == EstimateId);

    private bool CanWork => SalesAccess.CanWork(Session.ActiveRole);
    private bool Editable => CanWork && Estimate is { } e && e.Status.IsOpen();

    protected override async Task OnInitializedAsync()
    {
        Detail.OnChanged += StateHasChanged;
        CostCenters.OnChanged += StateHasChanged;
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        sessionReady = true;
        await LoadAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!sessionReady || loadedForId == EstimateId) return;
        dataFailed = false;
        actionNote = null;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        loadedForId = EstimateId;
        try
        {
            await Task.WhenAll(
                Detail.RefreshAsync(LeadId, CancellationToken.None),
                CostCenters.IsLoaded ? Task.CompletedTask : CostCenters.RefreshAsync(CancellationToken.None));
            Seed();
        }
        catch { dataFailed = true; }
    }

    // The drafts are seeded from the record once per load (and after every save) — never on a
    // re-render, so a keystroke is never wiped by the read model waking up.
    private void Seed()
    {
        if (Estimate is not { } estimate) return;
        breakdown = EstimateBreakdownDraft.From(estimate);
        seeding++;
        dirty = false;
    }

    private async Task ReloadAsync()
    {
        try { await Detail.RefreshAsync(LeadId, CancellationToken.None); Seed(); }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    public void Dispose()
    {
        Detail.OnChanged -= StateHasChanged;
        CostCenters.OnChanged -= StateHasChanged;
    }
}
