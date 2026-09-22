namespace Jewel.JPMS.Pages;

public partial class ProfitSummary
{
    // How the profit table is read (the readability brief, 2026-09-21): the identity column and
    // the totals row are always pinned; Compact fits every band onto a 1440-wide laptop and is
    // remembered per user, and its margin and memo lines come back with Show detail for the
    // visit. Below the md breakpoint the same rows read as cards.
    private bool isCompactTable;
    private bool showCompactDetail;

    private async Task LoadTableViewAsync() =>
        isCompactTable = await ViewStorage.ReadAsync(Auth.CurrentUser!.Email);

    private async Task OnCompactChangedAsync(bool isCompact)
    {
        isCompactTable = isCompact;
        await ViewStorage.WriteAsync(Auth.CurrentUser!.Email, isCompact);
    }
}
