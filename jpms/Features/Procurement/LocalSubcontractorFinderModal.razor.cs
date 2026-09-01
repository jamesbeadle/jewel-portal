
namespace Jewel.JPMS.Features.Procurement;

/// <summary>What the finder hands back: the ticked companies and the trade term the search used.</summary>
public sealed record LocalSubcontractorPick(IReadOnlyList<LocalSubcontractor> Places, string Trade);

public partial class LocalSubcontractorFinderModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter, EditorRequired] public string BidPackageId { get; set; } = "";
    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";

    /// <summary>The package's own trade — the search term when resolution cannot do better.</summary>
    [Parameter] public string FallbackTrade { get; set; } = "";

    /// <summary>The host's in-flight flag while the confirm's commands run.</summary>
    [Parameter] public bool Busy { get; set; }

    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<LocalSubcontractorPick> OnConfirm { get; set; }

    private bool resolving;
    private bool searchBusy;
    private bool hasSearched;
    private string trade = "";
    private string? searchError;
    private string? tradeNote;
    private string? notReadyReason;
    private string? nextPageToken;
    private readonly List<LocalSubcontractor> results = new();
    private readonly Dictionary<string, LocalSubcontractor> selection = new(StringComparer.Ordinal);
    private bool wasOpen;

    /// <summary>Each opening starts clean, resolves the trade, then auto-searches — as the page did.</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen == wasOpen) return;
        wasOpen = IsOpen;
        if (!IsOpen) return;

        trade = "";
        results.Clear();
        selection.Clear();
        nextPageToken = null;
        searchError = null;
        hasSearched = false;
        tradeNote = null;
        notReadyReason = null;
        resolving = true;
        StateHasChanged();

        try
        {
            var resolution = await Queries.AskAsync(new ResolveBidPackageTrade(BidPackageId), CancellationToken.None);
            if (resolution is null || !resolution.Ready)
            {
                notReadyReason = resolution?.Reason
                    ?? "This package needs its details (under Details) before subcontractors are invited.";
            }
            else
            {
                trade = resolution.Trade ?? FallbackTrade;
                tradeNote = resolution.UsedAi
                    ? "Worked out from the package's title and details — edit it and press Search if it's off."
                    : resolution.Reason;
            }
        }
        catch
        {
            trade = FallbackTrade;
            tradeNote = "The trade couldn't be worked out just now — using the package's own trade.";
        }
        finally
        {
            resolving = false;
        }
        // Repaint BEFORE the auto-search: Blazor only re-renders an async handler at its first
        // yield and its end, so without this the modal keeps saying "Working out the trade…"
        // through the whole web search — which reads as a hang.
        StateHasChanged();

        if (notReadyReason is null && !string.IsNullOrWhiteSpace(trade))
            await RunSearch(loadMore: false);
    }

    private async Task RunSearch(bool loadMore)
    {
        if (searchBusy || string.IsNullOrWhiteSpace(trade)) return;
        searchBusy = true;
        searchError = null;
        // Explicit repaint so the button's spinner shows even when this is called mid-handler
        // (the auto-search after trade resolution) rather than as its own click event.
        StateHasChanged();
        try
        {
            var result = await Queries.AskAsync(
                new SearchLocalSubcontractors(ProjectId, trade.Trim(), loadMore ? nextPageToken : null),
                CancellationToken.None);
            if (!loadMore)
            {
                results.Clear();
                selection.Clear();
            }
            if (result.Error is not null)
            {
                searchError = result.Error;
                nextPageToken = null;
            }
            else
            {
                results.AddRange(result.Results.Where(hit => results.All(existing => existing.PlaceId != hit.PlaceId)));
                nextPageToken = result.NextPageToken;
            }
            hasSearched = true;
        }
        catch { searchError = "The search failed. Please try again."; }
        finally { searchBusy = false; }
    }

    private void TogglePlace(LocalSubcontractor place, ChangeEventArgs e)
    {
        if (e.Value is true) selection[place.PlaceId] = place;
        else selection.Remove(place.PlaceId);
    }

    private Task ConfirmAsync() =>
        OnConfirm.InvokeAsync(new LocalSubcontractorPick(selection.Values.ToList(), trade));
}
