using System.Globalization;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>How the valuation report's figures read — quantities and rates, percentages, the
/// signed movement — shared by the report table, its rows and its summary.</summary>
public static class ValuationReportDisplay
{
    public static readonly CultureInfo Gb = CultureInfo.GetCultureInfo("en-GB");

    public static string Num(decimal value) => value.ToString("0.##", Gb);

    public static string Pct(decimal value) => value.ToString("0.##", Gb) + "%";

    /// <summary>The "what changed" delta: "+20%", "-5%" (negatives already carry their sign).</summary>
    public static string SignedPct(decimal value) => (value > 0m ? "+" : "") + value.ToString("0.##", Gb) + "%";

    /// <summary>A percentage as an editor is seeded with it: every decimal place that is stored,
    /// trailing zeros trimmed. Seeding with the 2dp display figure was what silently rounded a
    /// 33.3333% line to 33.33% the moment anyone clicked away from its editor.</summary>
    public static string PercentText(decimal value) => value.ToString("0.############################", Gb);
}
