using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Features.Labour.MyDay;

/// <summary>Asks for the cost code the day's words point to, once the worker pauses typing
/// (Jeremy, 1 Oct 2026). Each new keystroke withdraws the ask before it; an ask that fails or is
/// withdrawn answers nothing, so the picker is only ever filled, never emptied, by a suggestion.</summary>
public sealed class MyDayCodeSuggester
{
    private static readonly TimeSpan Pause = TimeSpan.FromMilliseconds(900);

    private readonly ILabourStore labour;
    private CancellationTokenSource? pending;

    public MyDayCodeSuggester(ILabourStore labour) { this.labour = labour; }

    public MyDayCostCodeSuggestion Latest { get; private set; } = new();

    public bool IsSuggested(string costCode) => Latest.HasCode && Latest.CostCode == costCode;

    public async Task<MyDayCostCodeSuggestion?> AfterPauseAsync(string projectId, string words)
    {
        pending?.Cancel();
        var own = pending = new CancellationTokenSource();
        try
        {
            await Task.Delay(Pause, own.Token);
            Latest = await labour.SuggestCostCodeAsync(projectId, words);
            return Latest;
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception) { return null; }
    }
}
