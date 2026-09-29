using System.Net;
using System.Text.RegularExpressions;

namespace Jewel.JPMS.Components;

/// <summary>
/// The inline markdown one line may carry — bold, italics, inline code, links — rendered onto the
/// design-system classes after the line is HTML-encoded, so a plan or a manual module can never
/// inject markup. Links are http(s) only.
/// </summary>
public static class MarkdownInline
{
    private static readonly Regex Bold = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
    private static readonly Regex Italic = new(@"(?<![\*\w])\*(?!\s)(.+?)(?<!\s)\*(?!\w)", RegexOptions.Compiled);
    private static readonly Regex Code = new(@"`([^`]+)`", RegexOptions.Compiled);
    private static readonly Regex MarkdownLink = new(@"\[([^\]]+)\]\((https?://[^\s)]+)\)", RegexOptions.Compiled);
    private static readonly Regex BareUrl = new(@"(?<![""'>=\]\(])(https?://[^\s<)\]]+)", RegexOptions.Compiled);
    private static readonly char[] SentencePunctuation = { '.', ',', ';', ':' };
    private const string LinkClass = "text-info hover:underline underline-offset-4 break-all";

    public static string Render(string line)
    {
        var encoded = WebUtility.HtmlEncode(line);
        encoded = Code.Replace(encoded, "<code class=\"text-xs bg-surface-raised px-1 rounded\">$1</code>");
        encoded = Bold.Replace(encoded, "<strong class=\"text-content font-medium\">$1</strong>");
        encoded = Italic.Replace(encoded, "<em>$1</em>");
        encoded = MarkdownLink.Replace(encoded, "<a class=\"" + LinkClass + "\" href=\"$2\" target=\"_blank\" rel=\"noopener\">$1</a>");
        return BareUrl.Replace(encoded, LinkTheAddress);
    }

    private static string LinkTheAddress(Match match)
    {
        var url = match.Value.TrimEnd(SentencePunctuation);
        var tail = match.Value[url.Length..];
        return "<a class=\"" + LinkClass + "\" href=\"" + url + "\" target=\"_blank\" rel=\"noopener\">" + url + "</a>" + tail;
    }
}
