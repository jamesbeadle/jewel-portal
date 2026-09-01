namespace Jewel.JPMS.Features.Cashflow;

/// <summary>How the cashflow pages' figures read on screen — the weekly plan and the cash
/// forecast share their cells, defined once.</summary>
public static class CashflowDisplay
{
    // Grid cells drop the pennies: fourteen columns of £1,234.56 is noise. Totals underneath
    // stay exact — the display rounds, the arithmetic never does.
    public static string CellAmount(decimal value) =>
        value == 0m ? "—" : value.ToString("C0", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));

    public static string Signed(decimal value) =>
        value == 0m ? "—"
        : (value > 0m ? "+" : "") + value.ToString("C0", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));

    public static string WeekLabel(DateTimeOffset weekStart, int index) =>
        index == 0 ? "This week" : weekStart.UtcDateTime.ToString("d MMM");

    public static string MonthLabel(DateTime month) =>
        month.ToString("MMM yy", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
}
