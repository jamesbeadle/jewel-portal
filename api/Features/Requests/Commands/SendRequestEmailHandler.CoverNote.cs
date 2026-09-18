using Jewel.JPMS.Api.Features.Requests.Documents;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed partial class SendRequestEmailHandler
{
    /// <summary>The short branded HTML cover note — mirrors the worker's outbound send so a drafted
    /// email reads the same as an auto-issued one. Internal so the reply-draft path
    /// (<see cref="SendRequestReplyHandler"/>) reuses the identical note.</summary>
    internal static string BuildCoverNote(RequestDocumentModel model)
    {
        var due = model.ResponseDue is { } responseDue
            ? $"<p style=\"margin:0 0 12px\">A response is requested by <strong>{responseDue:dd MMM yyyy}</strong>.</p>"
            : string.Empty;

        // Lead with the client-visible reference (RFI-012) — matching the subject line, the PDF and
        // the register — not the internal REQ number. The type word is only added in the pre-numbering
        // fallback, where DisplayReference is the bare REQ number.
        var displayReference = !string.IsNullOrWhiteSpace(model.Reference)
            ? model.Reference
            : $"{model.TypeShort} {model.DisplayNumber}".Trim();

        return $@"<div style=""font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#1A1E29;line-height:1.5"">
  <p style=""margin:0 0 12px"">Please find attached <strong>{displayReference}</strong> &mdash; {System.Net.WebUtility.HtmlEncode(model.Title)} &mdash; for project {System.Net.WebUtility.HtmlEncode(model.ProjectName)} ({System.Net.WebUtility.HtmlEncode(model.ProjectReference)}).</p>
  {due}
  <p style=""margin:0 0 12px"">The attached PDF contains the full details and any references. Please reply to this email to respond.</p>
  <p style=""margin:16px 0 0;color:#C09A51;font-weight:bold"">Jewel Bespoke Build</p>
  <p style=""margin:0;color:#FF8300;font-size:12px"">jewelbb.co.uk</p>
</div>";
    }
}
