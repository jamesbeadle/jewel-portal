using Jewel.JPMS.Contracts.Documents.Excel;
using static Jewel.JPMS.Contracts.WeeklyCashflow.Export.WeeklyCashflowExportStyles;

namespace Jewel.JPMS.Contracts.WeeklyCashflow.Export;

/// <summary>The rows every grid tab is built from: the heading row, section and band rows, a
/// line, and the running answer at the foot.</summary>
internal sealed partial class WeeklyCashflowExportGrid
{
    public const string NetMovementLabel = "Net movement";
    public const string ClosingBalanceLabel = "Closing bank balance";

    /// <summary>The column headings — the tab's own on the left, then every week, Later and Total.
    /// Everything down to this row stays frozen while the body scrolls.</summary>
    public void AddHeadingRow(params string[] leadingHeadings)
    {
        var cells = Cells(ColumnHead);
        for (var index = 0; index < leadingHeadings.Length && index < leadingColumnCount; index++)
            cells[index] = new ExcelStyledCell(leadingHeadings[index], ColumnHead);
        for (var cellIndex = 0; cellIndex < CellCount; cellIndex++)
            cells[CellColumn(cellIndex)] = new ExcelStyledCell(CellHeading(cellIndex), ColumnHeadRight);
        cells[TotalColumn] = new ExcelStyledCell(TotalHeading, ColumnHeadRight);
        AddRow(cells);
        Sheet.FrozenRows = Sheet.Rows.Count;
    }

    public void AddSectionHeading(string title)
    {
        var cells = Cells(SectionHeadFill);
        cells[0] = new ExcelStyledCell(title, SectionHead);
        AddRow(cells);
    }

    /// <summary>A band's totals row — the figures every line beneath it adds up to.</summary>
    public void AddBandRow(WeeklyCashflowExportBand band)
    {
        var cells = Cells(TotalFill);
        cells[0] = new ExcelStyledCell(band.Label, TotalLabel);
        for (var cellIndex = 0; cellIndex < CellCount; cellIndex++)
        {
            var amount = band.AmountIn(cellIndex);
            if (amount != 0m) cells[CellColumn(cellIndex)] = new ExcelStyledCell(amount, TotalMoneyFor(amount));
        }
        cells[TotalColumn] = new ExcelStyledCell(band.Total, TotalMoneyFor(band.Total));
        AddRow(cells);
    }

    public void AddLineRow(WeeklyCashflowExportLine line, ExcelCellStyle labelStyle)
    {
        var cells = LineCells(line.Label, labelStyle);
        for (var cellIndex = 0; cellIndex < CellCount; cellIndex++)
            PlaceAmount(cells, cellIndex, line.AmountIn(cellIndex), line.HasMovedEntryIn(cellIndex));
        PlaceTotal(cells, line.Total);
        AddRow(cells);
    }

    /// <summary>A line's blank row, hairlined across, with its label in the first cell.</summary>
    public object?[] LineCells(string label, ExcelCellStyle labelStyle)
    {
        var cells = Cells(Line);
        cells[0] = new ExcelStyledCell(label, labelStyle);
        return cells;
    }

    /// <summary>Nothing in a cell stays blank — a grid of £0.00 is noise, as on screen.</summary>
    public void PlaceAmount(object?[] cells, int cellIndex, decimal amount, bool isMoved)
    {
        if (amount == 0m) return;
        cells[CellColumn(cellIndex)] = new ExcelStyledCell(amount, Money(isMoved, amount < 0m));
    }

    public void PlaceTotal(object?[] cells, decimal total) =>
        cells[TotalColumn] = new ExcelStyledCell(total, Money(false, total < 0m));

    /// <summary>The running answer: cash in less cash out per week, Later included, and the total
    /// across the whole horizon.</summary>
    public void AddNetRow()
    {
        var cells = Cells(TotalFill);
        cells[0] = new ExcelStyledCell(NetMovementLabel, TotalLabel);
        for (var cellIndex = 0; cellIndex < CellCount; cellIndex++)
            cells[CellColumn(cellIndex)] = new ExcelStyledCell(view.Net[cellIndex], TotalMoneyFor(view.Net[cellIndex]));
        var total = view.Net.Sum();
        cells[TotalColumn] = new ExcelStyledCell(total, TotalMoneyFor(total));
        AddRow(cells);
    }

    /// <summary>Directors only: the running bank balance after each visible week. Later is beyond
    /// the horizon and not in the balance, so its cell and the total stay empty.</summary>
    public void AddClosingRow(IReadOnlyList<decimal> closing)
    {
        var cells = Cells(TotalFill);
        cells[0] = new ExcelStyledCell(ClosingBalanceLabel, TotalLabel);
        for (var weekIndex = 0; weekIndex < closing.Count; weekIndex++)
            cells[CellColumn(weekIndex)] = new ExcelStyledCell(closing[weekIndex], TotalMoneyFor(closing[weekIndex]));
        AddRow(cells);
    }
}
