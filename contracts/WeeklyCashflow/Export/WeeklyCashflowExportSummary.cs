using Jewel.JPMS.Contracts.Documents.Excel;
using static Jewel.JPMS.Contracts.WeeklyCashflow.Export.WeeklyCashflowExportStyles;

namespace Jewel.JPMS.Contracts.WeeklyCashflow.Export;

/// <summary>The page's tiles, as a block of label · figure · note rows: the directors' bank-anchored
/// set, or the movement-only set everyone else sees — mirroring the API's gate on the bank position.</summary>
internal static class WeeklyCashflowExportSummary
{
    public const string CashInBankLabel = "Cash in bank";
    public const string ToPayThisWeekLabel = "To pay this week";
    public const string LowestWeekLabel = "Lowest week";
    public const string HorizonEndLabel = "Balance at horizon end";
    private const string BankSourceNote = "Xero";
    private const string OverdueNote = "incl. everything overdue";
    private const string NeedsBankNote = "Needs the bank position from Xero";
    private const string CashOutNote = "bills and manual items in the visible weeks";
    private const string CashInNote = "outstanding invoices due in the visible weeks";
    private const int LabelColumn = 0;
    private const int FigureColumn = 1;
    private const int NoteColumn = 2;

    public static void Add(WeeklyCashflowExportGrid grid, WeeklyCashflowExportInput input)
    {
        AddTiles(grid, input);
        grid.AddBlankRow();
    }

    private static void AddTiles(WeeklyCashflowExportGrid grid, WeeklyCashflowExportInput input)
    {
        if (input.IsDirector)
        {
            AddDirectorRows(grid, input);
            return;
        }
        AddMovementRows(grid, input.View);
    }

    private static void AddDirectorRows(WeeklyCashflowExportGrid grid, WeeklyCashflowExportInput input)
    {
        var view = input.View;
        AddRow(grid, CashInBankLabel, input.CashInBank, input.CashInBank is null ? NeedsBankNote : BankSourceNote);
        AddRow(grid, ToPayThisWeekLabel, view.CashOut[0], OverdueNote);
        if (view.Closing is not { } closing)
        {
            AddRow(grid, LowestWeekLabel, null, NeedsBankNote);
            AddRow(grid, HorizonEndLabel, null, NeedsBankNote);
            return;
        }
        var lowestIndex = view.MinClosingIndex;
        AddRow(grid, LowestWeekLabel, closing[lowestIndex], WeeklyCashflowExportGrid.WeekHeading(view.WeekStarts[lowestIndex]));
        AddRow(grid, HorizonEndLabel, closing[^1], $"after {WeeklyCashflowExportGrid.WeekHeading(view.WeekStarts[^1])}");
    }

    private static void AddMovementRows(WeeklyCashflowExportGrid grid, WeeklyCashflowView view)
    {
        var weekCount = view.WeekStarts.Count;
        AddRow(grid, ToPayThisWeekLabel, view.CashOut[0], OverdueNote);
        AddRow(grid, $"Cash out, {weekCount} weeks", VisibleTotal(view.CashOut), CashOutNote);
        AddRow(grid, $"Cash in, {weekCount} weeks", VisibleTotal(view.CashIn), CashInNote);
    }

    // The visible weeks' total — the Later bucket, always last, left out (as the tiles do).
    private static decimal VisibleTotal(IReadOnlyList<decimal> totals) =>
        totals
            .Take(totals.Count - 1)
            .Sum();

    private static void AddRow(WeeklyCashflowExportGrid grid, string label, decimal? figure, string note)
    {
        var cells = new object?[grid.ColumnCount];
        cells[LabelColumn] = new ExcelStyledCell(label, SummaryLabel);
        if (figure is { } amount) cells[FigureColumn] = new ExcelStyledCell(amount, SummaryMoneyFor(amount));
        cells[NoteColumn] = new ExcelStyledCell(note, SummaryNote);
        grid.AddRow(cells);
    }
}
