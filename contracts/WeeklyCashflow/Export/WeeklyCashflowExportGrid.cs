using System.Globalization;
using Jewel.JPMS.Contracts.Documents.Excel;

namespace Jewel.JPMS.Contracts.WeeklyCashflow.Export;

/// <summary>
/// The week grid both grid tabs share: the tab's own leading text columns, then one column per
/// week, Later and Total. It owns where a cell goes, so the plan and detail tabs lay their money
/// out identically and a reader can address a column by meaning rather than by letter. Cell
/// indexes run over the week axis with Later last, exactly as <see cref="WeeklyCashflowView"/>
/// counts its columns.
/// </summary>
internal sealed partial class WeeklyCashflowExportGrid
{
    public const string LaterHeading = "Later";
    public const string TotalHeading = "Total";
    private const string WeekHeadingPrefix = "w/c ";
    private const string WeekHeadingDateFormat = "d MMM";
    // Room for a bold seven-figure amount — Excel prints ######## when a number outgrows its column.
    private const double WeekColumnWidth = 14;
    private const double TotalColumnWidth = 15;
    private const int FrozenLabelColumns = 1;

    private readonly WeeklyCashflowView view;
    private readonly int leadingColumnCount;

    public WeeklyCashflowExportGrid(
        ExcelWorkbook workbook, string sheetName, WeeklyCashflowView view, IReadOnlyList<ExcelColumn> leadingColumns)
    {
        this.view = view;
        leadingColumnCount = leadingColumns.Count;
        var columns = new List<ExcelColumn>(leadingColumns);
        columns.AddRange(view.WeekStarts.Select(weekStart => new ExcelColumn(WeekHeading(weekStart), ExcelFormat.Currency, WeekColumnWidth)));
        columns.Add(new ExcelColumn(LaterHeading, ExcelFormat.Currency, WeekColumnWidth));
        columns.Add(new ExcelColumn(TotalHeading, ExcelFormat.Currency, TotalColumnWidth));

        // A presentation sheet: the tabs draw their own headings, and the label column stays put
        // while the weeks scroll.
        Sheet = workbook.AddSheet(sheetName, columns.ToArray());
        Sheet.ShowHeaderRow = false;
        Sheet.AutoFilter = false;
        Sheet.FreezeHeaderRow = false;
        Sheet.ShowGridLines = false;
        Sheet.PrintLandscapeFitToWidth = true;
        Sheet.FrozenColumns = FrozenLabelColumns;
    }

    public ExcelSheet Sheet { get; }

    public int ColumnCount => Sheet.Columns.Count;

    /// <summary>The week cells plus Later.</summary>
    public int CellCount => view.WeekStarts.Count + 1;

    public int CellColumn(int cellIndex) => leadingColumnCount + cellIndex;

    public int TotalColumn => ColumnCount - 1;

    public static string WeekHeading(DateTimeOffset weekStart) =>
        WeekHeadingPrefix + weekStart.UtcDateTime.ToString(WeekHeadingDateFormat, CultureInfo.InvariantCulture);

    public string CellHeading(int cellIndex) =>
        cellIndex == view.LaterIndex ? LaterHeading : WeekHeading(view.WeekStarts[cellIndex]);

    /// <summary>A whole row of empty cells in one style — the canvas every styled row is painted on.</summary>
    public object?[] Cells(ExcelCellStyle style) =>
        Enumerable
            .Repeat<object?>(new ExcelStyledCell(null, style), ColumnCount)
            .ToArray();

    public void AddRow(object?[] cells) => Sheet.AddRow(cells);

    public void AddBlankRow() => Sheet.AddRow();

    /// <summary>One cell of text merged across the whole width.</summary>
    public void AddMergedRow(ExcelStyledCell first)
    {
        Sheet.AddRow(first);
        Sheet.MergedRanges.Add($"A{Sheet.Rows.Count}:{ColumnLetterAt(ColumnCount - 1)}{Sheet.Rows.Count}");
    }

    /// <summary>The Excel letter of a 0-based column index.</summary>
    public static string ColumnLetterAt(int columnIndex) => ExcelWorkbookWriter.ColumnLetter(columnIndex + 1);
}
