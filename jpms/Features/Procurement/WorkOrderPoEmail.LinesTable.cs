using System.Globalization;
using System.Text;

namespace Jewel.JPMS.Features.Procurement;

public static partial class WorkOrderPoEmail
{
    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");

    /// <summary>One displayed line of the order summary table. Mirrors WorkOrderLine's display
    /// fields so callers that only have staged input (Control Centre) can build rows the same way
    /// the server will store them (quantity 1, unit "item", total = amount).</summary>
    public sealed record Line(string Title, decimal Quantity, string Unit, decimal LineTotal);

    public static Line ToLine(WorkOrderLine line) => new(line.Title, line.Quantity, line.Unit, line.LineTotal);

    /// <summary>The priced lines as the supplier reads them, shared by the purchase-order email and
    /// the tender award email so an order's figures never read two ways.</summary>
    public static string LinesTable(IReadOnlyList<Line> lines, decimal orderTotal)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse\">");
        sb.AppendLine("<tr><th align=\"left\">Item</th><th align=\"left\">Qty</th><th align=\"left\">Unit</th><th align=\"right\">Total</th></tr>");
        foreach (var line in lines)
            sb.AppendLine($"<tr><td>{line.Title}</td><td>{QuantityText(line.Quantity)}</td><td>{line.Unit}</td><td align=\"right\">{line.LineTotal:£#,##0.00}</td></tr>");
        sb.AppendLine($"<tr><td colspan=\"3\"><strong>Order total</strong></td><td align=\"right\"><strong>{orderTotal:£#,##0.00}</strong></td></tr>");
        sb.AppendLine("</table>");
        return sb.ToString();
    }

    /// <summary>Quantities are stored at the scale the form accepts, so 92 bags read "92.0000" in
    /// every email until they were formatted — the purchase order sheet has always printed them
    /// this way (WorkOrderPoRenderer.Lines).</summary>
    public static string QuantityText(decimal quantity) => quantity.ToString("0.##", Uk);
}
