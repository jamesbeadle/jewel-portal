using System.Globalization;

namespace Jewel.JPMS.Features.Cvr;

/// <summary>How the profit figures read on screen — shared by the Profit Summary page, its
/// panels and the running-profit grid, defined once so none of them can round differently.</summary>
public static class ProfitDisplay
{
    public static string ProfitClass(decimal value) =>
        value == 0m ? "text-content-muted" : value > 0m ? "text-positive" : "text-negative";

    /// <summary>A signed whole-pound figure ("+£205,958", "-£12,400"); zero reads as a dash.</summary>
    public static string SignedMoney(decimal value) =>
        value == 0m ? "—" : value > 0m ? $"+£{value:N0}" : $"-£{Math.Abs(value):N0}";

    /// <summary>A margin fraction read as a percentage with one decimal ("9.1%").</summary>
    public static string Pct(decimal fraction) => $"{fraction * 100m:0.0}%";

    /// <summary>An inline-style percentage, culture-invariant — a comma decimal separator would silently break the CSS.</summary>
    public static string Pc(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>Signed £k with one decimal — the trajectory's six-month headline ("+4.7", "−12.0").</summary>
    public static string DeltaK(decimal value) =>
        value >= 0m ? $"+{value / 1000m:0.0}" : $"−{Math.Abs(value) / 1000m:0.0}";

    /// <summary>The cumulative chart's line pair — the accountant's mock colours, validated for
    /// CVD separation and contrast on the card surface.</summary>
    public const string InvoicedLineColor = "#3987e5";
    public const string CostLineColor = "#d95926";

    public static string MoneyCompact(decimal value)
    {
        var sign = value < 0m ? "-" : "";
        var abs = Math.Abs(value);
        return abs >= 1_000_000m ? $"{sign}£{abs / 1_000_000m:0.00}m"
            : abs >= 10_000m ? $"{sign}£{abs / 1_000m:0}k"
            : abs >= 1_000m ? $"{sign}£{abs / 1_000m:0.0}k"
            : $"{sign}£{abs:N0}";
    }

    /// <summary>Signed compact £ for the grid's hovers ("+£8.1k", "−£4.8k").</summary>
    public static string SignedMoneyCompact(decimal value) =>
        value >= 0m ? $"+{MoneyCompact(value)}" : MoneyCompact(value);

    /// <summary>The grid's unit: a percentage with one decimal ("26.5%", "−39.0%"), the accountant's rounding.</summary>
    public static string PctCell(decimal value) =>
        value >= 0m ? $"{value:0.0}%" : $"−{Math.Abs(value):0.0}%";

    /// <summary>A movement in percentage points, one decimal, always signed ("+15.3", "−11.8") — Jeremy's small print.</summary>
    public static string SignedPp(decimal value) =>
        value >= 0m ? $"+{value:0.0}" : $"−{Math.Abs(value):0.0}";

    /// <summary>The small-print line: the movement, or "—" for no meaningful movement (or no prior % to move from).</summary>
    public static string MovementPrint(decimal? movementPp) =>
        movementPp is decimal move && Math.Abs(move) >= 0.05m ? SignedPp(move) : "—";

    /// <summary>The grid's cell shading: green improving, red worsening, alpha scaling with the movement's share of the biggest (capped).</summary>
    public static string MovementCellStyle(decimal movementPp, decimal shadeMax)
    {
        if (shadeMax <= 0m) return "background:#12151c";
        var alpha = (0.10m + 0.55m * Math.Min(Math.Abs(movementPp), shadeMax) / shadeMax)
            .ToString("0.00", CultureInfo.InvariantCulture);
        return movementPp < 0m
            ? $"background:rgba(194,85,85,{alpha})"
            : $"background:rgba(46,160,101,{alpha})";
    }

    /// <summary>The running cell's hover: the position to date, then the month's own figures — the old grid's cell, preserved.</summary>
    public static string RunningCellHover(DateTime month, RunningCell cell)
    {
        var own = cell.Own.Percent is decimal ownPct
            ? $"this month: invoiced {MoneyCompact(cell.Own.Income)} · profit {SignedMoneyCompact(cell.Own.Profit)} ({PctCell(ownPct)})"
            : $"this month: nothing invoiced · profit {SignedMoneyCompact(cell.Own.Profit)}";
        return $"{month:MMM yy} — to date: invoiced {MoneyCompact(cell.CumIncome)} · profit {SignedMoneyCompact(cell.CumProfit)} · {own}";
    }
}
