using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed partial class SubcontractorStatementEmailComposer
{
    private static string DefaultSubject(SubcontractorStatement statement) =>
        $"Statement of account — Jewel Bespoke Build — {statement.GeneratedAt.LocalDateTime:d MMM yyyy}";

    /// <summary>The cover note travels as HTML in the draft; the statement detail itself is the
    /// attached PDF, so the note only summarises the account and invites reconciliation.</summary>
    private static string DefaultBody(SubcontractorStatement statement)
    {
        var contact = string.IsNullOrWhiteSpace(statement.ContactName) ? statement.CompanyName : statement.ContactName;
        var note = new System.Text.StringBuilder();
        note.AppendLine($"<p>Dear {Encoded(contact)},</p>");
        note.AppendLine("<p>Please find attached your current statement of account with Jewel Bespoke Build — the work orders we hold with you and the invoices claimed against each of them.</p>");
        if (statement.Projects.Count > 0) note.AppendLine(ProjectsTable(statement));
        note.AppendLine("<p>All figures are net of VAT; credit notes are shown as deductions. The attached PDF breaks each work order down to the individual invoices claimed against it.</p>");
        note.AppendLine("<p>If anything on the statement doesn't match your records, please reply to this email and we'll reconcile it together.</p>");
        note.AppendLine("<p>Kind regards,<br/>Jewel Bespoke Build</p>");
        return note.ToString();
    }

    private static string ProjectsTable(SubcontractorStatement statement)
    {
        var table = new System.Text.StringBuilder();
        table.AppendLine("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse\">");
        table.AppendLine("<tr><th align=\"left\">Project</th><th align=\"right\">Orders</th><th align=\"right\">Ordered</th><th align=\"right\">Invoiced</th><th align=\"right\">Remaining</th></tr>");
        foreach (var project in statement.Projects)
        {
            var name = string.IsNullOrWhiteSpace(project.ProjectReference)
                ? project.ProjectName
                : $"{project.ProjectReference} — {project.ProjectName}";
            table.AppendLine(
                $"<tr><td>{Encoded(name)}</td><td align=\"right\">{project.Orders.Count}</td>"
                + $"<td align=\"right\">{project.Ordered:£#,##0.00}</td><td align=\"right\">{project.Invoiced:£#,##0.00}</td>"
                + $"<td align=\"right\">{project.Remaining:£#,##0.00}</td></tr>");
        }
        table.AppendLine(
            $"<tr><td><strong>Account total</strong></td><td align=\"right\"><strong>{statement.OrderCount}</strong></td>"
            + $"<td align=\"right\"><strong>{statement.TotalOrdered:£#,##0.00}</strong></td>"
            + $"<td align=\"right\"><strong>{statement.TotalInvoiced:£#,##0.00}</strong></td>"
            + $"<td align=\"right\"><strong>{statement.TotalRemaining:£#,##0.00}</strong></td></tr>");
        table.AppendLine("</table>");
        return table.ToString();
    }

    /// <summary>A company's own name reaches the note as text, never as markup.</summary>
    private static string Encoded(string text) => System.Net.WebUtility.HtmlEncode(text);
}
