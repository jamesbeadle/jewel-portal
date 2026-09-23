using System.Text;
using Jewel.JPMS.Contracts.MailboxCompose;

namespace Jewel.JPMS.Features.Procurement;

/// <summary>
/// The purchase-order covering email to the supplier, composed once for every route that emails a
/// work order: the PO page's "Email to supplier…" modal (pre-fill, editable) and the automatic
/// send fired when an order is released — created without "save as draft" (Work Orders tab, Control
/// Centre) or a draft approved. One builder means the supplier reads the same email whichever door
/// the order went out through: the order summary (priced lines, or value + scope when there is no
/// breakdown), programme dates when set, and the standard pre-start paperwork line (RAMS/insurances
/// to projects@ — never a named person). The purchase order PDF is attached by the API on both the
/// send and the draft (2026-09-09, the accountant's ask), so the body says so. The acceptance
/// link is NOT composed here: the API mints the order's token on the first send and inserts the
/// acceptance paragraph above the sign-off on the way out (WorkOrderAcceptanceEmailParagraph,
/// 2026-09-23), so the supplier accepts from the email with no portal login and no door ever
/// sees the token. Keep the sign-off paragraph — it is where the API puts the link.
/// </summary>
public static partial class WorkOrderPoEmail
{
    // Scope is plain text typed in the work-order form; the PO sheet prints it pre-wrap. The
    // email body is HTML, where raw newlines collapse into spaces — encode then convert breaks,
    // so a breakdown typed one charge per line stays one charge per line here too.
    private static string AsHtmlLines(string text) =>
        System.Net.WebUtility.HtmlEncode(text.Trim()).Replace("\r\n", "\n").Replace("\n", "<br/>");

    public static string Subject(WorkOrder order, string projectName) =>
        $"Work order {order.Reference} — {SubjectLine.NamingTheProjectOnce(order.Title, projectName)}";

    /// <summary>The same subject, naming the tender package the order was awarded from, so the
    /// supplier reads which of their tenders it answers. The award page composed its own line
    /// until 2026-09-18 and carried neither the order's own reference rule nor the project.</summary>
    public static string SubjectForTenderAward(WorkOrder order, string projectName, string packageReference) =>
        $"{Subject(order, projectName)} ({packageReference})";

    public static string Body(
        WorkOrder order,
        string supplierName,
        IReadOnlyList<Line> lines,
        string projectName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<p>Hello {supplierName},</p>");
        sb.AppendLine($"<p>Please find below the details of our work order <strong>{order.Reference}</strong> for <strong>{(string.IsNullOrWhiteSpace(projectName) ? "the project" : projectName)}</strong>. The purchase order is attached.</p>");
        sb.AppendLine(lines.Count > 0 ? LinesTable(lines, order.Value) : ValueAndScope(order));
        if (order.ProgrammeStart is { } start)
            sb.AppendLine($"<p><strong>Programme start:</strong> {start.LocalDateTime:d MMM yyyy}</p>");
        if (order.ScheduledCompletion is { } completion)
            sb.AppendLine($"<p><strong>Scheduled completion:</strong> {completion.LocalDateTime:d MMM yyyy}</p>");
        sb.AppendLine("<p>Before starting on site, please send your RAMS documentation and current insurance certificates to projects@jewelbb.co.uk.</p>");
        sb.AppendLine("<p>Kind regards,<br/>Jewel Bespoke Build</p>");
        return sb.ToString();
    }

    private static string ValueAndScope(WorkOrder order)
    {
        var value = $"<p><strong>Order value:</strong> {order.Value:£#,##0.00}</p>";
        if (string.IsNullOrWhiteSpace(order.Scope)) return value;
        return value + Environment.NewLine + $"<p><strong>Scope:</strong><br/>{AsHtmlLines(order.Scope)}</p>";
    }
}
