using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;
using Jewel.JPMS.Features.Xero;


namespace Jewel.JPMS.Pages;

public partial class ProfitSummary
{
    // ---- Bridge -------------------------------------------------------------
    // Waterfall geometry precomputed here so the markup only places rectangles. Values are
    // mapped into a 12%–100% vertical band (the top 12% is reserved for the amount labels).

    private sealed record BridgeBar(
        string Label, string Sub, string Amount, string BarClass, string LabelClass,
        double Top, double Height, double? ConnectorY);

    private sealed record BridgeModel(string Note, double ZeroY, IReadOnlyList<BridgeBar> Bars);

    private BridgeModel? BridgeFor(IReadOnlyList<(Project Project, ProfitRow Row)> rows, ProfitRow total)
    {
        var budgeted = total.BudgetedProfit;
        var variations = total.NetVariations;
        var forecast = total.ForecastedProfit;
        var afterVariations = budgeted + variations;

        // The worst cost mover names the third bar — the board's first question is "who".
        var worstCost = rows.OrderBy(entry => entry.Row.CostMovement).FirstOrDefault();
        var costSub = worstCost.Project is not null && worstCost.Row.CostMovement < 0m && total.CostMovement < 0m
            ? $"{MoneyCompact(-worstCost.Row.CostMovement)} of this is {worstCost.Project.Name}"
            : "vs target cost";

        var segments = new (string Label, string Sub, decimal From, decimal To, bool Endpoint)[]
        {
            ("Budgeted profit", "as signed", 0m, budgeted, true),
            ("Variations", "approved only", budgeted, afterVariations, false),
            ("Cost movement", costSub, afterVariations, forecast, false),
            ("Forecast profit", "at completion", 0m, forecast, true),
        };

        var low = Math.Min(0m, segments.Min(segment => Math.Min(segment.From, segment.To)));
        var high = Math.Max(0m, segments.Max(segment => Math.Max(segment.From, segment.To)));
        if (high == low) return null; // nothing to plot — every figure is zero

        // Map a value into the band: 12% at the top of the range, 100% at the bottom.
        double Y(decimal value) => 12d + (double)((high - value) / (high - low)) * 88d;

        var bars = new List<BridgeBar>();
        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            var delta = segment.To - segment.From;
            var top = Y(Math.Max(segment.From, segment.To));
            var height = Math.Max(Y(Math.Min(segment.From, segment.To)) - top, 0.75d);
            var barClass = segment.Endpoint
                ? "bg-content-faint"
                : delta >= 0m ? "bg-positive/80" : "bg-negative/80";
            var labelClass = segment.Endpoint
                ? segment.To < 0m ? "text-negative" : "text-content"
                : delta >= 0m ? "text-positive" : "text-negative";
            var amount = segment.Endpoint ? Money(segment.To) : SignedMoney(delta);
            // The dashed hand-over line from the previous column, at the level it left off.
            double? connector = index == 0 ? null : Y(segments[index - 1].To);
            bars.Add(new BridgeBar(segment.Label, segment.Sub, amount, barClass, labelClass, top, height, connector));
        }

        var note = forecast < budgeted
            ? $"where {MoneyCompact(budgeted - forecast)} of margin went"
            : "how the deal improves by completion";
        return new BridgeModel(note, Y(0m), bars);
    }

}
