using System.Text;
using System.Text.RegularExpressions;

namespace Jewel.JPMS.Components;

/// <summary>
/// Renders the small markdown the portal writes and reads — headings, paragraphs, bullet and numbered
/// lists, pipe tables, and the inline marks in MarkdownInline — as HTML on the design-system classes.
/// Deliberately tiny: no nesting beyond one level; a document that needs more is edited as text.
/// </summary>
public sealed class MarkdownRenderer
{
    private const string OrderedList = "ol";
    private const string UnorderedList = "ul";
    private const int LargeHeadingLevels = 2;
    private static readonly Regex Numbered = new(@"^\s*\d+[.)]\s+(.*)$", RegexOptions.Compiled);
    private static readonly Regex Bulleted = new(@"^\s*[-*•]\s+(.*)$", RegexOptions.Compiled);
    private static readonly Regex Heading = new(@"^(#{1,6})\s+(.*)$", RegexOptions.Compiled);

    private readonly StringBuilder html = new();
    private readonly List<string> paragraph = new();
    private readonly MarkdownTable table = new();
    private string? listTag;

    public static string Render(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var renderer = new MarkdownRenderer();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n')) renderer.Take(raw.TrimEnd());
        renderer.CloseEverything();
        return renderer.html.ToString();
    }

    private void Take(string line)
    {
        if (line.Length == 0) { CloseEverything(); return; }
        if (MarkdownTable.IsRow(line)) { FlushParagraph(); CloseList(); table.Add(line); return; }
        table.WriteTo(html);
        var heading = Heading.Match(line);
        if (heading.Success) { TakeHeading(heading); return; }
        var numbered = Numbered.Match(line);
        if (numbered.Success) { TakeListItem(OrderedList, numbered.Groups[1].Value); return; }
        var bulleted = Bulleted.Match(line);
        if (bulleted.Success) { TakeListItem(UnorderedList, bulleted.Groups[1].Value); return; }
        CloseList();
        paragraph.Add(MarkdownInline.Render(line.Trim()));
    }

    private void TakeHeading(Match heading)
    {
        FlushParagraph();
        CloseList();
        var groups = heading.Groups;
        var marks = groups[1].Value;
        var level = marks.Length;
        var headingClass = level <= LargeHeadingLevels ? "text-lg font-semibold text-content pt-2" : "text-base font-medium text-content pt-1";
        html.Append("<h4 class=\"").Append(headingClass).Append("\">").Append(MarkdownInline.Render(groups[2].Value)).Append("</h4>");
    }

    private void TakeListItem(string tag, string item)
    {
        FlushParagraph();
        OpenList(tag);
        html.Append("<li>").Append(MarkdownInline.Render(item)).Append("</li>");
    }

    private void OpenList(string tag)
    {
        if (listTag == tag) return;
        CloseList();
        html.Append(tag == OrderedList ? "<ol class=\"list-decimal pl-5 space-y-1\">" : "<ul class=\"list-disc pl-5 space-y-1\">");
        listTag = tag;
    }

    private void CloseList()
    {
        if (listTag is null) return;
        html.Append("</").Append(listTag).Append('>');
        listTag = null;
    }

    private void FlushParagraph()
    {
        if (paragraph.Count == 0) return;
        html.Append("<p>").Append(string.Join(" ", paragraph)).Append("</p>");
        paragraph.Clear();
    }

    private void CloseEverything()
    {
        FlushParagraph();
        CloseList();
        table.WriteTo(html);
    }
}
