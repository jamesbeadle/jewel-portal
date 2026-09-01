using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Services.Excel;
using static Jewel.JPMS.Features.Commercial.ValuationExportStyles;

using static Jewel.JPMS.MoneyFormats;

namespace Jewel.JPMS.Features.Commercial;

/// <summary>One line of a pending variation's staged build-up — the client-agreed pricing captured before approval.</summary>
public sealed record ValuationExportPendingLine(string CostCode, string Description, decimal Quantity, decimal Rate)
{
    public decimal Amount => Quantity * Rate;
}

/// <summary>
/// A variation order still awaiting a decision, as the workbook's Pending tab shows it. The
/// status is the reason it is pending — Quoting, Issued or Awaiting AI, with the register's own
/// hint wording. Money is the staged build-up when one exists, else the order's estimate.
/// </summary>
public sealed record ValuationExportPendingVariation(
    string Reference,
    string Title,
    string StatusLabel,
    string StatusReason,
    decimal? Estimate,
    IReadOnlyList<ValuationExportPendingLine> Lines)
{
    public decimal EstimatedAmount => Lines.Count > 0 ? Lines.Sum(line => line.Amount) : Estimate ?? 0m;
}

/// <summary>
/// Maps the variations register to the export's pending rows: orders ISSUED to the client and
/// awaiting their decision — status Issued, or Awaiting AI (issued, waiting on the Architect's
/// Instruction) — in number order, and only those carrying a figure (a staged build-up or an
/// estimate). Quoting-stage orders are internal pricing, not a claim-in-waiting, so they stay
/// off entirely, as do unpriced placeholders (accountant's requests 2026-08-26). Rejected
/// orders are decided, not pending, and approved ones are on the report.
/// </summary>
public static class ValuationExportPendingVariations
{
    public static IReadOnlyList<ValuationExportPendingVariation> From(IEnumerable<VariationOrder> orders) =>
        orders
            .Where(order => order.Status is VariationOrderStatus.Issued or VariationOrderStatus.AwaitingArchitectInstruction)
            .Where(HasValue)
            .OrderBy(order => order.Number)
            .Select(order => new ValuationExportPendingVariation(
                order.Number > 0 ? VariationRefs.Padded(order.Number) : order.Reference,
                order.Title,
                order.Status.DisplayName(),
                order.Status.Hint(),
                order.EstimatedValue,
                (order.DraftLines ?? Array.Empty<VariationLineInput>())
                    .Select(line => new ValuationExportPendingLine(line.CostCode, line.Description, line.Quantity, line.Rate))
                    .ToList()))
            .ToList();

    private static bool HasValue(VariationOrder order) =>
        order.DraftLines is { Count: > 0 } || (order.EstimatedValue is { } estimate && estimate != 0m);
}

/// <summary>
/// The workbook's "Pending variations" tab: every order still awaiting a decision, its staged
/// build-up lines underneath, and its status as the reason it is pending. Nothing here has any
/// commercial effect — these figures are in no total on any other tab — and the list is read
/// from the live register at the moment of export (a snapshot freezes the report, not the
/// register, so this tab is stamped as an as-at-export view on both kinds of export).
/// </summary>
internal static class ValuationExportPendingSheet
{
    public const string SheetName = "Pending variations";

    private const string Legend =
        "Variations issued to the client and awaiting a decision — not approved, so nothing here is in the contract sum or any other tab · "
        + "Only issued orders carrying a figure are listed; quoting-stage pricing and unpriced placeholders are not · "
        + "Read from the variations register at the moment of export · The status on each order is why it is pending · All figures net of VAT.";

    private static readonly ExcelCellStyle BandStatus = BandHead with { Align = ExcelAlign.Right, WrapText = true };
    private static readonly ExcelCellStyle NoteText = new(Font: ExcelFont.Muted, Border: ExcelBorder.Hairline, WrapText: true);

    public static void Add(ExcelWorkbook workbook, ValuationExportMeta meta, IReadOnlyList<ValuationExportPendingVariation>? pending)
    {
        var sheet = workbook.AddSheet(SheetName,
            new ExcelColumn("Code", Width: 13),
            new ExcelColumn("Description", Width: 52),
            new ExcelColumn("Cost centre", Width: 18),
            new ExcelColumn("Qty", Width: 9),
            new ExcelColumn("Rate", Width: 11),
            new ExcelColumn("Amount", Width: 14),
            new ExcelColumn("Status", Width: 40));
        ValuationExportStatementSheet.SetPresentationFlags(sheet);
        sheet.TabColour = AwaitingApprovalTabColour; // still awaiting approval
        ValuationExportTitleBand.Add(sheet, meta, Legend);

        // The register is live data: saying it couldn't be read beats passing an empty tab off
        // as "none pending" — same honesty rule as the loading gates on screen.
        if (pending is null)
        {
            AddNote(sheet, "The variations register couldn't be read when this export was taken, so pending variations are not shown.");
            return;
        }
        if (pending.Count == 0)
        {
            AddNote(sheet, "No issued variations are awaiting a decision — orders still being priced are not listed here.");
            return;
        }

        AddColumnHeadings(sheet);
        foreach (var order in pending)
        {
            AddOrder(sheet, order);
        }
        AddGrandTotal(sheet, pending);
    }

    private static void AddNote(ExcelSheet sheet, string note)
    {
        var cells = ValuationExportStatementSheet.FilledCells(sheet, Plain);
        cells[1] = new ExcelStyledCell(note, NoteText);
        sheet.AddRow(cells);
    }

    private static void AddColumnHeadings(ExcelSheet sheet) =>
        sheet.AddRow(
            new ExcelStyledCell("Code", ColHead),
            new ExcelStyledCell("Description", ColHead),
            new ExcelStyledCell("Cost centre", ColHead),
            new ExcelStyledCell("Qty", ColHeadRight),
            new ExcelStyledCell("Rate £", ColHeadRight),
            new ExcelStyledCell("Amount £", ColHeadRight),
            new ExcelStyledCell("Status — why it is pending", ColHeadRight));

    private static void AddOrder(ExcelSheet sheet, ValuationExportPendingVariation order)
    {
        // The order's band: ref and title on the left, its stage — the pending reason — on the right.
        var band = ValuationExportStatementSheet.FilledCells(sheet, BandFill);
        band[0] = new ExcelStyledCell(HeadingFor(order), BandHead);
        band[^1] = new ExcelStyledCell($"{order.StatusLabel} — {order.StatusReason}", BandStatus);
        sheet.AddRow(band);

        if (order.Lines.Count == 0)
        {
            // Nothing staged yet — the only figure the register holds is the estimate.
            sheet.AddRow(
                new ExcelStyledCell(null, Text(false)),
                new ExcelStyledCell("No agreed build-up staged — the figure is the order's estimate", NoteText),
                new ExcelStyledCell(null, Text(false)),
                new ExcelStyledCell(null, Text(false)),
                new ExcelStyledCell(null, Text(false)),
                new ExcelStyledCell(order.Estimate, Money(false)),
                new ExcelStyledCell(null, Text(false)));
        }
        else
        {
            foreach (var line in order.Lines)
            {
                sheet.AddRow(
                    new ExcelStyledCell(null, Text(false)),
                    new ExcelStyledCell(line.Description, Desc(false)),
                    new ExcelStyledCell(line.CostCode, Code(false)),
                    new ExcelStyledCell(line.Quantity, Num(false)),
                    new ExcelStyledCell(line.Rate, Num(false)),
                    new ExcelStyledCell(line.Amount, Money(false, negative: line.Amount < 0m)),
                    new ExcelStyledCell(null, Text(false)));
            }
            var total = ValuationExportStatementSheet.FilledCells(sheet, TotalFill);
            total[1] = new ExcelStyledCell($"{order.Reference} total (est.)", TotalLabel);
            total[5] = new ExcelStyledCell(order.EstimatedAmount, TotalMoney);
            sheet.AddRow(total);
        }
        sheet.AddRow();
    }

    private static void AddGrandTotal(ExcelSheet sheet, IReadOnlyList<ValuationExportPendingVariation> pending)
    {
        var cells = ValuationExportStatementSheet.FilledCells(sheet, TotalFill);
        cells[1] = new ExcelStyledCell("Total pending variations (estimates)", TotalLabel);
        cells[5] = new ExcelStyledCell(pending.Sum(order => order.EstimatedAmount), TotalMoney);
        sheet.AddRow(cells);
    }

    private static string HeadingFor(ValuationExportPendingVariation order) =>
        string.Join(" — ", new[] { order.Reference, order.Title }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part.Trim()));
}
