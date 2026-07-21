namespace Jewel.JPMS.Services.Excel;

/// <summary>
/// Number/display format applied to a column when exporting to Excel.
/// </summary>
public enum ExcelFormat
{
    /// <summary>Excel's General format — used for plain text and untyped values.</summary>
    General,
    /// <summary>Whole numbers with thousands separators (#,##0).</summary>
    Integer,
    /// <summary>Two-decimal numbers with thousands separators (#,##0.00).</summary>
    Number,
    /// <summary>Pounds sterling (£#,##0.00).</summary>
    Currency,
    /// <summary>UK date (dd/mm/yyyy).</summary>
    Date,
    /// <summary>UK date and time (dd/mm/yyyy hh:mm).</summary>
    DateTime,
    /// <summary>Percentage with one decimal (0.0%). Supply values as fractions, e.g. 0.42 for 42%.</summary>
    Percent,
}

/// <summary>
/// A column definition for an exported sheet. Width is in Excel character units;
/// leave null to size automatically from the header and cell contents.
/// </summary>
public sealed record ExcelColumn(string Header, ExcelFormat Format = ExcelFormat.General, double? Width = null);

/// <summary>
/// A single worksheet: a styled header row followed by data rows.
/// Row cells are matched to columns by position; nulls render as blank cells.
/// </summary>
public sealed class ExcelSheet
{
    public ExcelSheet(string name, IReadOnlyList<ExcelColumn> columns)
    {
        Name = name;
        Columns = columns;
    }

    public string Name { get; }
    public IReadOnlyList<ExcelColumn> Columns { get; }
    public List<object?[]> Rows { get; } = new();

    public void AddRow(params object?[] cells) => Rows.Add(cells);
}

/// <summary>
/// A workbook to export: one or more sheets. Build with collection or object
/// initializer syntax, then hand to <see cref="ExcelExportService"/>.
/// </summary>
public sealed class ExcelWorkbook
{
    public List<ExcelSheet> Sheets { get; } = new();

    public ExcelSheet AddSheet(string name, params ExcelColumn[] columns)
    {
        var sheet = new ExcelSheet(name, columns);
        Sheets.Add(sheet);
        return sheet;
    }
}
