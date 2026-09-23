using System.Text.Json;

namespace Jewel.JPMS.Models;

/// <summary>
/// A table question's answer travels and is stored as one JSON string under the question's key —
/// an array of rows, each a column key to its cell — so the answers dictionary every part of the
/// engine already passes around carries a register exactly as it carries a name. Reading is
/// tolerant: anything that is not that shape is no rows.
/// </summary>
public static class FormTableAnswers
{
    public const int LongestCell = 512;

    public static IReadOnlyList<IReadOnlyDictionary<string, string>> Read(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<IReadOnlyDictionary<string, string>>();
        try
        {
            var rows = JsonSerializer.Deserialize<List<Dictionary<string, string?>>>(json);
            return rows is null ? Array.Empty<IReadOnlyDictionary<string, string>>() : rows.Select(WithoutNulls).ToList();
        }
        catch (JsonException)
        {
            return Array.Empty<IReadOnlyDictionary<string, string>>();
        }
    }

    public static string Write(IReadOnlyList<IReadOnlyDictionary<string, string>> rows) =>
        rows.Count == 0 ? "" : JsonSerializer.Serialize(rows);

    /// <summary>What the form keeps of what was posted: the table's own columns only, each cell trimmed and capped; a
    /// checklist keeps exactly its fixed rows with the definition's labels re-imposed; a free table drops a row nobody
    /// wrote on and stops at its row limit.</summary>
    public static IReadOnlyList<IReadOnlyDictionary<string, string>> Cleaned(FormTable table, string json)
    {
        var posted = Read(json);
        if (table.HasFixedRows) return table.FixedRows.Select((fixedRow, index) => CleanedFixedRow(table, fixedRow, RowAt(posted, index))).ToList();
        return posted.Select(row => CleanedRow(table, row)).Where(row => IsWrittenOn(table, row)).Take(table.MostRows).ToList();
    }

    public static bool HasAnAnswer(FormTable table, string json) => Read(json).Any(row => IsWrittenOn(table, row));

    /// <summary>The rows as a person reads them in an email or on a printed record: one line per row, the typed cells
    /// with their headings, a checklist row led by its label.</summary>
    public static string Sentence(FormTable table, string json)
    {
        var rows = Read(json);
        var lines = rows.Select((row, index) => RowLine(table, row, index + 1)).Where(line => line.Length > 0);
        return string.Join(Environment.NewLine, lines);
    }

    private static string RowLine(FormTable table, IReadOnlyDictionary<string, string> row, int number)
    {
        var cells = table.Columns
            .Select(column => (column, value: row.GetValueOrDefault(column.Key, "").Trim()))
            .Where(cell => cell.value.Length > 0)
            .Select(cell => cell.column.Kind == FormColumnKind.Label ? cell.value : $"{cell.column.Label}: {cell.value}")
            .ToList();
        if (!IsWrittenOn(table, row)) return "";
        return $"{number}. {string.Join(" · ", cells)}";
    }

    private static IReadOnlyDictionary<string, string> CleanedFixedRow(
        FormTable table, IReadOnlyDictionary<string, string> fixedRow, IReadOnlyDictionary<string, string> posted)
    {
        var row = CleanedRow(table, posted);
        foreach (var label in table.Columns.Where(column => !column.IsTyped)) row[label.Key] = fixedRow.GetValueOrDefault(label.Key, "");
        return row;
    }

    private static Dictionary<string, string> CleanedRow(FormTable table, IReadOnlyDictionary<string, string> posted)
    {
        var row = new Dictionary<string, string>();
        foreach (var column in table.Columns)
        {
            var cell = Capped(posted.GetValueOrDefault(column.Key, ""), column);
            if (cell.Length > 0) row[column.Key] = cell;
        }
        return row;
    }

    private static string Capped(string cell, FormColumn column)
    {
        var trimmed = cell.Trim();
        if (column.Kind == FormColumnKind.Tick) return trimmed == FormWording.Yes ? FormWording.Yes : "";
        return trimmed.Length > LongestCell ? trimmed[..LongestCell] : trimmed;
    }

    private static bool IsWrittenOn(FormTable table, IReadOnlyDictionary<string, string> row) =>
        table.TypedColumns.Any(column => row.GetValueOrDefault(column.Key, "").Trim().Length > 0);

    private static IReadOnlyDictionary<string, string> RowAt(IReadOnlyList<IReadOnlyDictionary<string, string>> rows, int index) =>
        index < rows.Count ? rows[index] : new Dictionary<string, string>();

    private static IReadOnlyDictionary<string, string> WithoutNulls(Dictionary<string, string?> row) =>
        row.Where(pair => pair.Value is not null).ToDictionary(pair => pair.Key, pair => pair.Value!);
}
