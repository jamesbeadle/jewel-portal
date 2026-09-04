namespace Jewel.JPMS.Contracts.Documents.Excel;

public static partial class ExcelWorkbookWriter
{
    private const int HeaderRowCount = 1;

    /// <summary>
    /// The sheet's frozen pane, or null for none: the classic header-row freeze on a data sheet,
    /// otherwise the leading rows and columns a presentation grid asks for. Excel wants the
    /// scrolling corner named — bottom-right when both axes are split, else the one that is.
    /// </summary>
    private static string? FrozenPaneXml(ExcelSheet sheet)
    {
        var hasHeaderFreeze = sheet.ShowHeaderRow && sheet.FreezeHeaderRow;
        var rows = hasHeaderFreeze ? HeaderRowCount : sheet.FrozenRows;
        var columns = hasHeaderFreeze ? 0 : sheet.FrozenColumns;
        if (rows <= 0 && columns <= 0) return null;

        var splits = "";
        if (columns > 0) splits += $" xSplit=\"{columns}\"";
        if (rows > 0) splits += $" ySplit=\"{rows}\"";
        var topLeftCell = $"{ColumnLetter(columns + 1)}{rows + 1}";
        return $"<pane{splits} topLeftCell=\"{topLeftCell}\" activePane=\"{ActivePane(rows, columns)}\" state=\"frozen\"/>";
    }

    private static string ActivePane(int frozenRows, int frozenColumns)
    {
        if (frozenRows > 0 && frozenColumns > 0) return "bottomRight";
        if (frozenRows > 0) return "bottomLeft";
        return "topRight";
    }
}
