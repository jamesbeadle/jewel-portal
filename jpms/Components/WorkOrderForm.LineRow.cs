using System.Globalization;

namespace Jewel.JPMS.Components;

public partial class WorkOrderForm
{
    public sealed class LineRow
    {
        // Ties the row to an existing line so its paid-to-date and invoice history survive the
        // edit; null for rows added in this session.
        public string? WorkOrderLineId { get; set; }
        public decimal PaidToDate { get; set; }
        public string CostCode { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string AmountText { get; set; } = "";
        // The measured breakdown ("14" / "m2" / "54.00"): all optional, but quantity and rate
        // come as a pair — when both parse, the amount is DERIVED (qty × rate) and its input
        // locks, so the printed Qty/Unit, Unit Cost and Price columns can never disagree.
        public string QuantityText { get; set; } = "";
        public string Unit { get; set; } = "";
        public string UnitCostText { get; set; } = "";

        /// <summary>A stored line back into the boxes. A real measured breakdown round-trips into
        /// qty/unit/rate; the long-standing "1 item" placeholder stays out of them — showing it
        /// would dress every legacy line up as measured.</summary>
        public static LineRow From(WorkOrderLine line)
        {
            var isPlaceholder = line.Quantity == 1m && line.Unit == "item";
            return new LineRow
            {
                WorkOrderLineId = line.WorkOrderLineId,
                PaidToDate = line.PaidToDate,
                CostCode = line.CostCode,
                Title = line.Title,
                Description = line.Description,
                AmountText = line.LineTotal.ToString(CultureInfo.InvariantCulture),
                QuantityText = isPlaceholder ? "" : line.Quantity.ToString(CultureInfo.InvariantCulture),
                Unit = isPlaceholder ? "" : line.Unit,
                UnitCostText = isPlaceholder ? "" : line.UnitCost.ToString(CultureInfo.InvariantCulture)
            };
        }
    }

    /// <summary>True when the line carries a full measured breakdown — a positive quantity and a
    /// parseable rate. Only then do quantity, unit and rate travel to the server; a lone
    /// quantity or lone rate is a validation problem, not a silent "1 item".</summary>
    internal static bool IsMeasured(LineRow line) =>
        Parse(line.QuantityText) is { } quantity && quantity > 0m && Parse(line.UnitCostText) is not null;

    /// <summary>Qty × rate, kept in the amount box whenever both halves parse.</summary>
    private static void RecalculateAmount(LineRow line)
    {
        if (Parse(line.QuantityText) is { } quantity && quantity > 0m
            && Parse(line.UnitCostText) is { } rate)
        {
            line.AmountText = Math.Round(quantity * rate, 2).ToString(CultureInfo.InvariantCulture);
        }
    }

    internal static decimal? Parse(string text) =>
        decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    internal static string Money(decimal value) =>
        value.ToString("C2", CultureInfo.GetCultureInfo("en-GB"));
}
