using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Contracts.MailboxCompose;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed partial class ValuationSnapshotEmailComposer
{
    private static string DefaultSubject(ValuationReportSnapshot snapshot, string? projectName) =>
        SubjectLine.NamingTheProjectOnce($"Valuation report — {snapshot.Label}", projectName ?? "");

    /// <summary>The cover note travels as HTML in the draft; the report detail itself is the
    /// attached PDF, so the note only summarises the position and invites queries. It reads the
    /// snapshot's frozen figures, so what the client is told in the body and what the PDF prints
    /// cannot disagree.</summary>
    private static string DefaultBody(ValuationReportSnapshot snapshot, string? projectName)
    {
        var note = new System.Text.StringBuilder();
        note.AppendLine("<p>Dear all,</p>");
        note.AppendLine(string.IsNullOrWhiteSpace(projectName)
            ? $"<p>Please find attached the valuation report — {snapshot.Label} — as it stood on {snapshot.TakenAt.LocalDateTime:d MMM yyyy}.</p>"
            : $"<p>Please find attached the valuation report for {Encoded(projectName)} — {snapshot.Label} — as it stood on {snapshot.TakenAt.LocalDateTime:d MMM yyyy}.</p>");
        note.AppendLine("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse\">");
        note.AppendLine(Row("Original contract sum", snapshot.ContractSum));
        note.AppendLine(Row("Net variations", snapshot.NetVariations));
        note.AppendLine(Row("Revised contract sum", snapshot.RevisedContractSum, strong: true));
        note.AppendLine(Row("Total works complete", snapshot.TotalWorksComplete));
        note.AppendLine(Row("Retention held", snapshot.RetentionHeld));
        note.AppendLine(Row("Retention released", snapshot.RetentionReleased));
        note.AppendLine(Row("Certified to date", snapshot.CertifiedToDate));
        note.AppendLine(Row("Payment due (ex VAT)", snapshot.PaymentDueExVat, strong: true));
        note.AppendLine("</table>");
        note.AppendLine("<p>All figures are net of VAT. The attached PDF breaks the position down line by line — the contract works, provisional and contingency sums, and variations, each with its claimed value to date.</p>");
        note.AppendLine("<p>If you have any queries on the figures, please reply to this email and we'll pick them up.</p>");
        note.AppendLine("<p>Kind regards,<br/>Jewel Bespoke Build</p>");
        return note.ToString();
    }

    private static string Row(string label, decimal amount, bool strong = false) =>
        strong
            ? $"<tr><td><strong>{label}</strong></td><td align=\"right\"><strong>{amount:£#,##0.00}</strong></td></tr>"
            : $"<tr><td>{label}</td><td align=\"right\">{amount:£#,##0.00}</td></tr>";

    private static string Encoded(string text) => System.Net.WebUtility.HtmlEncode(text);
}
