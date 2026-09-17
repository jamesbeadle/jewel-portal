using Jewel.JPMS.Contracts.Sales;
using Jewel.JPMS.Features.Sales;

namespace Jewel.JPMS.Pages;

public partial class SalesStrategyDetail
{
    // While the research is queued or running the page re-reads the strategy every few
    // seconds, so the findings and plan appear without a reload; the timer stops itself as
    // soon as the status settles, and on navigating away.
    private CancellationTokenSource? polling;

    private void EnsurePolling()
    {
        if (Strategy is not { } strategy || !strategy.ResearchStatus.IsInProgress() || polling is not null) return;
        polling = new CancellationTokenSource();
        _ = PollAsync(polling.Token);
    }

    private async Task PollAsync(CancellationToken ct)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(6));
            while (await timer.WaitForNextTickAsync(ct))
            {
                try { await Detail.RefreshAsync(StrategyId, ct); }
                catch (OperationCanceledException) { throw; }
                catch { continue; } // the toast has the detail; try again next tick
                if (Strategy is not { } current || !current.ResearchStatus.IsInProgress()) break;
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            polling?.Dispose();
            polling = null;
            // The status settled: the strategies list's card carries it too.
            try { await Strategies.RefreshAsync(CancellationToken.None); } catch { }
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task RunResearchAsync()
    {
        if (busy) return;
        busy = true; actionError = null;
        try
        {
            await Commands.SendAsync(new RunStrategyResearch(StrategyId), CancellationToken.None);
            await ReloadAsync();
            EnsurePolling();
        }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; }
    }
}
