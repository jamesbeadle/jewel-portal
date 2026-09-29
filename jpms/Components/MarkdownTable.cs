using System.Text;

namespace Jewel.JPMS.Components;

/// <summary>
/// A pipe table — a header row, its dashed separator, then rows — rendered as a small table on the
/// design-system classes. Anything the renderer feeds it is one line of the table; cells go through
/// the inline renderer, so they are encoded like every other line.
/// </summary>
public sealed class MarkdownTable
{
    private const char Pipe = '|';
    private readonly List<string[]> rows = new();

    public static bool IsRow(string line) => line.StartsWith(Pipe) && line.EndsWith(Pipe);

    public void Add(string line)
    {
        var cells = line.Trim(Pipe).Split(Pipe).Select(cell => cell.Trim()).ToArray();
        var isSeparator = cells.All(cell => cell.Length > 0 && cell.All(character => character == '-' || character == ':'));
        if (isSeparator) return;
        rows.Add(cells);
    }

    public bool IsEmpty => rows.Count == 0;

    public void WriteTo(StringBuilder html)
    {
        if (IsEmpty) return;
        html.Append("<table class=\"markdown-table text-sm w-full\"><thead><tr>");
        foreach (var cell in rows[0]) html.Append("<th class=\"text-left font-medium text-content pr-3 pb-1\">").Append(MarkdownInline.Render(cell)).Append("</th>");
        html.Append("</tr></thead><tbody>");
        foreach (var row in rows.Skip(1)) WriteRow(html, row);
        html.Append("</tbody></table>");
        rows.Clear();
    }

    private static void WriteRow(StringBuilder html, string[] row)
    {
        html.Append("<tr>");
        foreach (var cell in row) html.Append("<td class=\"align-top pr-3 py-1 border-t border-line\">").Append(MarkdownInline.Render(cell)).Append("</td>");
        html.Append("</tr>");
    }
}
