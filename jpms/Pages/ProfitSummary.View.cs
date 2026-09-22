namespace Jewel.JPMS.Pages;

public partial class ProfitSummary
{
    // How the page is read (the two readability briefs, 2026-09-21), remembered per user: the
    // profit table's Compact fit — its margin and memo lines come back with Show detail for the
    // visit — and the running grid's window length. Below the md breakpoint the table's rows
    // read as cards.
    private ProfitSummaryView view = ProfitSummaryView.Default;
    private bool showCompactDetail;

    private bool IsCompactTable => view.IsCompactTable;
    private int RunningWindowMonths => view.RunningWindowMonths;

    private async Task LoadViewAsync() =>
        view = await ViewStorage.ReadAsync(Auth.CurrentUser!.Email);

    private Task OnCompactChangedAsync(bool isCompact) =>
        SaveViewAsync(view with { IsCompactTable = isCompact });

    private Task OnWindowMonthsChangedAsync(int months) =>
        SaveViewAsync(view with { RunningWindowMonths = months });

    private async Task SaveViewAsync(ProfitSummaryView changed)
    {
        view = changed;
        await ViewStorage.WriteAsync(Auth.CurrentUser!.Email, changed);
    }
}
