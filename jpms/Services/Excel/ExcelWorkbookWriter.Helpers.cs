using System.Globalization;
using System.Text;

namespace Jewel.JPMS.Services.Excel;

public static partial class ExcelWorkbookWriter
{
    private static double EstimateWidth(ExcelSheet sheet, int columnIndex)
    {
        var longest = sheet.Columns[columnIndex].Header.Length;
        var sampled = 0;
        foreach (var row in sheet.Rows)
        {
            if (sampled++ >= 100) break;
            if (columnIndex >= row.Length) continue;
            var length = CellTextLength(row[columnIndex]);
            if (length > longest) longest = length;
        }
        // +3 leaves room for the autofilter dropdown on the header
        return Math.Clamp(longest + 3, 10, 60);
    }

    private static int CellTextLength(object? value) => value switch
    {
        null => 0,
        ExcelStyledCell styled => CellTextLength(styled.Value),
        DateTimeOffset or DateTime or DateOnly => 10,
        decimal m => m.ToString("#,##0.00", CultureInfo.InvariantCulture).Length + 1,
        double d => d.ToString("#,##0.00", CultureInfo.InvariantCulture).Length + 1,
        _ => (value.ToString() ?? "").Length,
    };

    internal static string ColumnLetter(int columnNumber)
    {
        var letters = "";
        while (columnNumber > 0)
        {
            columnNumber--;
            letters = (char)('A' + columnNumber % 26) + letters;
            columnNumber /= 26;
        }
        return letters;
    }

    private static string SanitizeSheetName(string name, int index, HashSet<string> used)
    {
        var cleaned = new string(name.Where(ch => ch is not ('[' or ']' or ':' or '*' or '?' or '/' or '\\')).ToArray()).Trim('\'', ' ');
        if (cleaned.Length == 0) cleaned = $"Sheet{index + 1}";
        if (cleaned.Length > 31) cleaned = cleaned[..31];
        var candidate = cleaned;
        var suffix = 2;
        while (!used.Add(candidate))
        {
            var tail = $" ({suffix++})";
            candidate = cleaned.Length + tail.Length > 31 ? cleaned[..(31 - tail.Length)] + tail : cleaned + tail;
        }
        return candidate;
    }

    private static string Escape(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '&': builder.Append("&amp;"); break;
                case '<': builder.Append("&lt;"); break;
                case '>': builder.Append("&gt;"); break;
                case '"': builder.Append("&quot;"); break;
                case '\t' or '\n' or '\r': builder.Append(ch); break;
                default:
                    if (ch < 0x20 || ch == 0xFFFE || ch == 0xFFFF) break; // drop control chars Excel rejects
                    builder.Append(ch);
                    break;
            }
        }
        return builder.ToString();
    }
}
