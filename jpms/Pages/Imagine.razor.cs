using System.Net.Http.Json;
using Jewel.JPMS.Features.Sales;

namespace Jewel.JPMS.Pages;

public partial class Imagine
{
    [Parameter] public string Token { get; set; } = "";

    private bool loading = true;
    private string? error;
    private ImagineView? view;
    private CancellationTokenSource? polling;

    private bool AnyInProgress => view?.Rounds.Any(round => round.Status.IsInProgress()) ?? false;
    private IReadOnlyList<ImagineImageView> AllConcepts => view?.Rounds.SelectMany(round => round.Concepts).ToList() ?? new List<ImagineImageView>();

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
        loading = false;
        StartPollingIfNeeded();
    }

    private async Task LoadAsync()
    {
        try
        {
            using var response = await Http.GetAsync(ImagineRequests.BaseFor(Token));
            view = response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<ImagineView>() : null;
        }
        catch { view = null; }
    }

    private void StartPollingIfNeeded()
    {
        if (!AnyInProgress || polling is not null) return;
        polling = new CancellationTokenSource();
        _ = PollAsync(polling.Token);
    }

    private async Task PollAsync(CancellationToken ct)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(8));
            while (await timer.WaitForNextTickAsync(ct))
            {
                await LoadAsync();
                StateHasChanged();
                if (!AnyInProgress) break;
            }
        }
        catch (OperationCanceledException) { }
        finally { polling?.Dispose(); polling = null; }
    }

    private Task OnViewChanged(ImagineView updated)
    {
        view = updated;
        return Task.CompletedTask;
    }

    private Task OnChildError(string? message)
    {
        error = message;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        polling?.Cancel();
    }
}
