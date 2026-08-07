using System.Net;
using System.Text;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Sharing;

/// <summary>
/// The attach-vs-link decision, shared by every path that emails files (invite draft, triage
/// compose, request document drafts). Graph's upload sessions already move any SIZE of file onto
/// a draft — the ceiling this planner guards is Exchange's message-size limit, which refuses the
/// whole email once combined attachments pass ~25 MB (the same figure the compose flow has always
/// enforced). The split is deterministic and minimal: files stay attached until the total fits,
/// moving the largest to links first — a recipient gets as much in-inbox as the limit allows and
/// links only for what genuinely cannot ride along. Inline (cid) images are never linked; they are
/// part of the body.
/// </summary>
public static class EmailAttachmentPlanner
{
    /// <summary>Cap on the combined size of an email's attachments — the usual Exchange
    /// message-size ceiling. One number for the whole system.</summary>
    public const long MaxTotalAttachmentBytes = 25_000_000;

    /// <summary>The outcome of a split: what still rides on the email, and what becomes a link.</summary>
    public sealed record Plan(IReadOnlyList<MailboxDraftAttachment> Attach, IReadOnlyList<MailboxDraftAttachment> ToLink);

    /// <summary>
    /// Splits the attachments so that what remains attached fits the budget.
    /// <paramref name="reservedBytes"/> is space already spoken for on the same email (the official
    /// PDF a request draft always attaches, a compose body's inline images).
    /// </summary>
    public static Plan Split(IReadOnlyList<MailboxDraftAttachment> attachments, long reservedBytes = 0)
    {
        var budget = MaxTotalAttachmentBytes - reservedBytes;
        var attach = attachments.ToList();
        var toLink = new List<MailboxDraftAttachment>();

        while (attach.Count > 0 && attach.Sum(a => a.Content.LongLength) > budget)
        {
            var largest = attach
                .Where(a => !a.IsInline)
                .OrderByDescending(a => a.Content.LongLength)
                .FirstOrDefault();
            if (largest is null) break; // only inline images left — nothing linkable
            attach.Remove(largest);
            toLink.Add(largest);
        }

        return new Plan(attach, toLink);
    }

    /// <summary>
    /// The branded HTML block appended to an email body listing the shared files — name, size,
    /// link, and when the links stop working. Same Arial/inline-style idiom as the cover notes,
    /// so it renders identically in Outlook.
    /// </summary>
    public static string LinksHtmlBlock(IReadOnlyList<EmailFileShareLink> links, string heading = "Download links")
    {
        if (links.Count == 0) return string.Empty;

        var expires = links.Max(l => l.ExpiresAt);
        var sb = new StringBuilder();
        sb.Append(@"<div style=""font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#1A1E29;line-height:1.5;margin:16px 0 0;padding:12px 16px;border:1px solid #E4E0D5;border-radius:6px;background:#FAF8F4"">");
        sb.Append($@"<p style=""margin:0 0 8px""><strong>{WebUtility.HtmlEncode(heading)}</strong></p>");
        sb.Append(@"<p style=""margin:0 0 8px"">The following files are too large to attach, so they are shared as download links:</p>");
        sb.Append(@"<ul style=""margin:0 0 8px;padding-left:20px"">");
        foreach (var link in links)
        {
            sb.Append($@"<li style=""margin:0 0 4px""><a href=""{WebUtility.HtmlEncode(link.Url.AbsoluteUri)}"" style=""color:#C09A51"">{WebUtility.HtmlEncode(link.FileName)}</a> ({FormatSize(link.SizeBytes)})</li>");
        }
        sb.Append("</ul>");
        sb.Append($@"<p style=""margin:0;font-size:12px;color:#6B6B60"">These links expire on <strong>{expires:d MMMM yyyy}</strong> &mdash; please download the files before then.</p>");
        sb.Append("</div>");
        return sb.ToString();
    }

    /// <summary>"850 KB" / "12.3 MB" — the sizes shown next to each link.</summary>
    public static string FormatSize(long bytes) => bytes switch
    {
        >= 1_000_000 => $"{bytes / 1_000_000.0:0.#} MB",
        >= 1_000 => $"{bytes / 1_000.0:0} KB",
        _ => $"{bytes} B",
    };
}
