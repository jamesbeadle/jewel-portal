using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Variations.Documents;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed partial class SendVariationOrderEmailHandler
{
    /// <summary>An ad-hoc override addresses the email to that one address instead; otherwise the
    /// project's client side — its Client and Architect contacts — through the shared rule the
    /// valuation statement reads too.</summary>
    private async Task<List<MailboxDraftRecipient>> RecipientsForAsync(
        string projectId, string? recipientOverride, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(recipientOverride))
            return new List<MailboxDraftRecipient> { new(recipientOverride.Trim()) };

        var recipients = await ClientSideRecipients.ForProjectAsync(context, projectId, cancellationToken);
        if (recipients.Count > 0) return recipients;

        throw new InvalidOperationException(
            "The project has no client or architect contact with an email address — add one to the "
            + "project's contacts before emailing the variation order.");
    }

    /// <summary>The message the dispatcher stages. The record tag rides on the draft and survives
    /// the send, so the sent copy files itself under the variation and the reply inherits it — the
    /// stem is the register's own, because a second spelling would detach the mail.</summary>
    private async Task<MailboxDraftMessage> StagedMessageAsync(
        VariationOrderEntity variation,
        VariationDocumentModel model,
        List<MailboxDraftRecipient> recipients,
        CancellationToken cancellationToken)
    {
        var pdf = VariationDocumentRenderer.Render(model);
        var recordTag = await VariationTags.StemAsync(context, variation, cancellationToken);

        return new MailboxDraftMessage(
            To: recipients,
            Subject: model.EmailSubject,
            HtmlBody: CoverNote(model),
            Attachments: new[] { new MailboxDraftAttachment(model.FileName, "application/pdf", pdf) },
            Categories: new[] { TriageCategories.Marker, TriageCategories.ForRecord(recordTag), TriageCategories.Client });
    }

    /// <summary>The short branded cover note. It leads with the number the document goes out under
    /// — VO31, never the internal VOQ reference (Nigel, 2026-09-14) — and states the figure,
    /// because a variation is a request to spend and the recipient should not have to open the PDF
    /// to see how much.</summary>
    private static string CoverNote(VariationDocumentModel model)
    {
        var value = model.IsApproved ? model.ApprovedValue : model.EstimatedValue ?? model.LinesTotal;
        var valueLine = value > 0
            ? $"<p style=\"margin:0 0 12px\">{(model.IsApproved ? "Approved value" : "Value")}: <strong>{value:£#,##0.00}</strong> excluding VAT.</p>"
            : string.Empty;

        return $@"<div style=""font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#1A1E29;line-height:1.5"">
  <p style=""margin:0 0 12px"">Please find attached <strong>{Encoded(model.DocumentReference)}</strong> &mdash; {Encoded(model.Title)} &mdash; for project {Encoded(model.ProjectName)} ({Encoded(model.ProjectReference)}).</p>
  {valueLine}
  <p style=""margin:0 0 12px"">The attached PDF sets out the scope, the commercial basis, any programme impact and the cost breakdown. Please reply to this email to respond.</p>
  <p style=""margin:16px 0 0;color:#C09A51;font-weight:bold"">Jewel Bespoke Build</p>
  <p style=""margin:0;color:#FF8300;font-size:12px"">jewelbb.co.uk</p>
</div>";
    }

    /// <summary>The record's own words reach the note as text, never as markup — a title carrying
    /// "&" would otherwise break the paragraph for every recipient.</summary>
    private static string Encoded(string text) => System.Net.WebUtility.HtmlEncode(text);
}
