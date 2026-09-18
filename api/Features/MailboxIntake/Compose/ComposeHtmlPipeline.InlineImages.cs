using System.Text.RegularExpressions;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

public sealed partial class ComposeHtmlPipeline
{
    /// <summary>Every &lt;img src="data:…"&gt; becomes a proper inline fileAttachment and the src is
    /// rewritten to cid:{id}, because mail clients render cid images reliably while multi-megabyte
    /// data: URLs are stripped or refused by many of them.</summary>
    private static ComposedBody WithPastedImagesLifted(string sanitised)
    {
        var inline = new List<MailboxDraftAttachment>();
        var index = 0;
        var rewritten = DataImage.Replace(sanitised, match =>
        {
            index++;
            var attachment = InlineImage(match, index);
            inline.Add(attachment);
            return $"src=\"cid:{attachment.ContentId}\"";
        });
        return new ComposedBody(rewritten, inline);
    }

    /// <summary>Plain textarea text → draft HTML: encode each line, join with &lt;br&gt;, and leave a
    /// blank line before any quoted history the result is prepended to.</summary>
    public static string FromPlainText(string body) =>
        "<div>"
        + string.Join("<br>", (body ?? "").Replace("\r\n", "\n").Split('\n').Select(System.Net.WebUtility.HtmlEncode))
        + "</div><br>";

    private static MailboxDraftAttachment InlineImage(Match match, int index)
    {
        var contentType = match.Groups[1].Value;
        byte[] bytes;
        try { bytes = Convert.FromBase64String(match.Groups[2].Value.Trim()); }
        catch (FormatException)
        {
            throw new InvalidOperationException("A pasted image couldn't be read — remove it and paste it again.");
        }
        if (bytes.LongLength > MaxInlineImageBytes)
            throw new InvalidOperationException(
                "A pasted image is larger than 4 MB — attach it as a file instead of pasting it into the body.");

        var extension = contentType.Split('/').Last() switch
        {
            "jpeg" => "jpg",
            var suffix when suffix.Length is > 0 and <= 8 => suffix,
            _ => "png"
        };
        return new MailboxDraftAttachment(
            $"pasted-image-{index}.{extension}", contentType, bytes,
            IsInline: true, ContentId: $"pasted-{Guid.NewGuid():N}");
    }
}
