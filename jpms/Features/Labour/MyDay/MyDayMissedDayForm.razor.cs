namespace Jewel.JPMS.Features.Labour.MyDay;

/// <summary>The missed day's cost code, suggested from its words once the worker pauses, as the
/// sign-out form does; a code picked by hand is theirs and never overwritten.</summary>
public partial class MyDayMissedDayForm
{
    private const string SuggestedCodeHint = "Suggested from your words — change it if that's not the trade.";

    private bool isCodePickedByHand;
    private MyDayCodeSuggester? suggester;

    private MyDayCodeSuggester Suggester => suggester ??= new MyDayCodeSuggester(Labour);

    private string? CodeHint => !isCodePickedByHand && Suggester.IsSuggested(costCode) ? SuggestedCodeHint : null;

    private void CodePicked() => isCodePickedByHand = true;

    private async Task SuggestCodeAsync()
    {
        if (isCodePickedByHand) return;
        var suggestion = await Suggester.AfterPauseAsync(Day.ProjectId, words);
        if (suggestion is not { HasCode: true }) return;
        costCode = suggestion.CostCode;
        StateHasChanged();
    }
}
