using Jewel.JPMS.Commercial;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Cvr;
using Jewel.JPMS.Features.Xero;
using static Jewel.JPMS.Features.Cvr.ProfitDisplay;


namespace Jewel.JPMS.Pages;

public partial class ProfitSummary
{
    // ---- Formatting ---------------------------------------------------------

    private static string SignedMoney(decimal value) =>
        value == 0m ? "—" : value > 0m ? $"+£{value:N0}" : $"-£{Math.Abs(value):N0}";

    private static string Pct(decimal fraction) => $"{fraction * 100m:0.0}%";

    // Inline-style percentages must be culture-invariant — a comma decimal separator would
    // silently break the CSS.
    private static string Pc(double value) =>
        value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);



    public void Dispose()
    {
        Projects.OnChanged -= StateHasChanged;
        Summary.OnChanged -= StateHasChanged;
        WorkOrders.OnChanged -= StateHasChanged;
        Lines.OnChanged -= StateHasChanged;
        Claims.OnChanged -= StateHasChanged;
        ClaimEntries.OnChanged -= StateHasChanged;
        SitePnl.OnChanged -= StateHasChanged;
        // The throttle is deliberately NOT disposed: a load still in flight when the user
        // navigates away would Release() a disposed semaphore and fault the abandoned task.
        // An undisposed SemaphoreSlim (no wait-handle use) holds nothing worth reclaiming.
    }

}
