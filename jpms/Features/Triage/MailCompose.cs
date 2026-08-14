using Jewel.JPMS.Contracts.MailboxCompose;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Features.Triage;

/// <summary>
/// The composer helpers every mail-writing surface shares — the triage reply, the Outbox's lined-up
/// replies, the to-do composer and the record pages' reply widgets all parse recipients, judge
/// "has anything been written?" and shape multipart uploads the same way, defined once here.
/// </summary>
public static class MailCompose
{
    /// <summary>"a@x; B &lt;b@y&gt;, c@z" → addresses. Display names in angle brackets are
    /// tolerated and stripped.</summary>
    public static List<ComposeRecipient> ParseRecipients(string field) =>
        (field ?? "")
        .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(part =>
        {
            var open = part.LastIndexOf('<');
            var close = part.LastIndexOf('>');
            return open >= 0 && close > open ? part[(open + 1)..close].Trim() : part;
        })
        .Where(address => address.Contains('@'))
        .Select(address => new ComposeRecipient(address))
        .ToList();

    /// <summary>A contenteditable's "empty" is markup like &lt;div&gt;&lt;br&gt;&lt;/div&gt;: strip
    /// tags before judging — except an inline image, which is real content with no text at all.</summary>
    public static bool HtmlHasContent(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return false;
        if (html.Contains("<img", StringComparison.OrdinalIgnoreCase)) return true;
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
        return !string.IsNullOrWhiteSpace(System.Net.WebUtility.HtmlDecode(text));
    }

    /// <summary>The multipart file parts of a composed attachment set — the pieces picked from
    /// this computer, named by their part key for the compose endpoint.</summary>
    public static IReadOnlyList<(string PartName, IBrowserFile File)> UploadPartsOf(
        IReadOnlyList<ComposeDraftAttachment> attachments) =>
        attachments
            .Where(attachment => attachment.File is not null)
            .Select(attachment => (attachment.Key, attachment.File!))
            .ToList();

    /// <summary>"RE: " the subject once — an already-RE'd subject stays as it is.</summary>
    public static string ReplySubjectFor(string? subject) =>
        string.IsNullOrWhiteSpace(subject) ? "RE: (no subject)"
        : subject.TrimStart().StartsWith("RE:", StringComparison.OrdinalIgnoreCase) ? subject.Trim()
        : $"RE: {subject.Trim()}";

    /// <summary>"FW: " the subject once — an already-FW'd (or FWD'd) subject stays as it is.</summary>
    public static string ForwardSubjectFor(string? subject) =>
        string.IsNullOrWhiteSpace(subject) ? "FW: (no subject)"
        : subject.TrimStart().StartsWith("FW:", StringComparison.OrdinalIgnoreCase)
          || subject.TrimStart().StartsWith("FWD:", StringComparison.OrdinalIgnoreCase) ? subject.Trim()
        : $"FW: {subject.Trim()}";
}
