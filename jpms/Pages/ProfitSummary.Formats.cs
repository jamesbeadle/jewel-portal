using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Xero;

using static Jewel.JPMS.MoneyFormats;

namespace Jewel.JPMS.Pages;

public partial class ProfitSummary
{
    // ---- Formatting ---------------------------------------------------------

    private static string ProfitClass(decimal value) =>
        value == 0m ? "text-content-muted" : value > 0m ? "text-positive" : "text-negative";


    private static string SignedMoney(decimal value) =>
        value == 0m ? "—" : value > 0m ? $"+£{value:N0}" : $"-£{Math.Abs(value):N0}";

    private static string MoneyCompact(decimal value)
    {
        var sign = value < 0m ? "-" : "";
        var abs = Math.Abs(value);
        return abs >= 1_000_000m ? $"{sign}£{abs / 1_000_000m:0.00}m"
            : abs >= 10_000m ? $"{sign}£{abs / 1_000m:0}k"
            : abs >= 1_000m ? $"{sign}£{abs / 1_000m:0.0}k"
            : $"{sign}£{abs:N0}";
    }

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
