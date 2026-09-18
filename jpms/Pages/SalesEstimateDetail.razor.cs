using Jewel.JPMS.Contracts.Sales;
using Jewel.JPMS.Features.Sales;

namespace Jewel.JPMS.Pages;

public partial class SalesEstimateDetail
{
    [Parameter] public string LeadId { get; set; } = "";
    [Parameter] public string EstimateId { get; set; } = "";

    private bool sessionReady;
    private bool dataFailed;
    private bool loadDone;
    private string loadedForId = "";

    private EstimateBreakdownDraft breakdown = new();
    private int seeding;
    private string seededForId = "";

    private Lead? Lead => Detail.Current(LeadId)?.Lead;
    private LeadEstimate? Estimate => Detail.Current(LeadId)?.Estimates?.FirstOrDefault(e => e.EstimateId == EstimateId);

    private bool CanWork => SalesAccess.CanWork(Session.ActiveRole);

    /// <summary>The drafts hold the estimate that is on screen. Every save here is a full-record
    /// write, so an editor over a draft seeded from nothing would save blanks over the record —
    /// while this is false the page is read-only.</summary>
    private bool Seeded => Estimate is { } estimate && seededForId == estimate.EstimateId;

    private bool Editable => CanWork && Seeded && Estimate is { } e && e.Status.IsOpen();

    /// <summary>The record on screen is a cached copy this page could not refresh — say so, because
    /// what is saved from it is what was last read.</summary>
    private bool ShowingStale => dataFailed && Estimate is not null;

    protected override async Task OnInitializedAsync()
    {
        Detail.OnChanged += OnDetailChanged;
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
        loadDone = false;
        try
        {
            await Task.WhenAll(
                Detail.RefreshAsync(LeadId, CancellationToken.None),
                CostCenters.IsLoaded ? Task.CompletedTask : CostCenters.RefreshAsync(CancellationToken.None));
            dataFailed = false;
        }
        catch { dataFailed = true; }
        // Outside the catch: a record can be on screen without this refresh — cached by the lead
        // page — and the drafts are owed to the record, not to the fetch succeeding.
        SeedIfUnseeded();
        loadDone = true;
    }

    // The drafts are seeded from the record once per estimate (and again after every save) — never
    // on a re-render, so a keystroke is never wiped by the read model waking up.
    private void Seed()
    {
        if (Estimate is not { } estimate) return;
        seededForId = estimate.EstimateId;
        breakdown = EstimateBreakdownDraft.From(estimate);
        seeding++;
        dirty = false;
    }

    /// <summary>Seeds when the estimate on screen is not the one the drafts hold — the record
    /// arriving is the occasion, whether it came from this page's refresh or another's.</summary>
    private void SeedIfUnseeded()
    {
        if (!Seeded) Seed();
    }

    private void OnDetailChanged()
    {
        SeedIfUnseeded();
        StateHasChanged();
    }

    private async Task ReloadAsync()
    {
        try { await Detail.RefreshAsync(LeadId, CancellationToken.None); Seed(); }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    public void Dispose()
    {
        Detail.OnChanged -= OnDetailChanged;
        CostCenters.OnChanged -= StateHasChanged;
    }
}
