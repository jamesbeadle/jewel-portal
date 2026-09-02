using static Jewel.JPMS.Features.Commercial.ValuationInvoiceDisplay;

namespace Jewel.JPMS.Components;

public partial class ValuationInvoicesSection
{
    // Same rows and order as the table (cancelled invoices stay listed, greyed out).
    private ExcelWorkbook? BuildExportWorkbook(bool _)
    {
        if (invoices.Count == 0) return null;

        var workbook = new ExcelWorkbook();
        var sheet = workbook.AddSheet("Valuation invoices",
            new ExcelColumn("Ref"),
            new ExcelColumn("Period", ExcelFormat.Date),
            new ExcelColumn("Amount £", ExcelFormat.Currency),
            new ExcelColumn("Status"),
            new ExcelColumn("Paid £", ExcelFormat.Currency));

        foreach (var invoice in invoices)
        {
            sheet.AddRow(
                invoice.DisplayNumber,
                invoice.PeriodMonth.LocalDateTime,
                invoice.Amount,
                StatusLabel(invoice.Status),
                invoice.AmountPaid);
        }
        return workbook;
    }
}
