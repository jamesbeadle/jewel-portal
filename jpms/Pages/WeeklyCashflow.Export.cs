using Jewel.JPMS.Contracts.WeeklyCashflow;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.WeeklyCashflow;

namespace Jewel.JPMS.Pages;

public partial class WeeklyCashflow
{

    private ExcelWorkbook? BuildExportWorkbook(bool includeAllRows)
    {
        if (!GridReady || !XeroReady) return null;
        var view = BuildView();
        var workbook = new ExcelWorkbook();

        var weekColumns = new List<ExcelColumn> { new("Week") };
        weekColumns.Add(new ExcelColumn("Cash in", ExcelFormat.Currency));
        weekColumns.Add(new ExcelColumn("Cash out", ExcelFormat.Currency));
        weekColumns.Add(new ExcelColumn("Net", ExcelFormat.Currency));
        if (IsDirector && view.Closing is not null) weekColumns.Add(new ExcelColumn("Closing balance", ExcelFormat.Currency));
        var summary = workbook.AddSheet("By week", weekColumns.ToArray());
        for (var index = 0; index < view.WeekStarts.Count; index++)
        {
            var row = new List<object?>
            {
                $"w/c {view.WeekStarts[index].UtcDateTime:dd/MM/yyyy}",
                view.CashIn[index],
                view.CashOut[index],
                view.Net[index]
            };
            if (IsDirector && view.Closing is not null) row.Add(view.Closing[index]);
            summary.Rows.Add(row.ToArray());
        }
        var laterRow = new List<object?> { "Later", view.CashIn[^1], view.CashOut[^1], view.Net[^1] };
        if (IsDirector && view.Closing is not null) laterRow.Add(null);
        summary.Rows.Add(laterRow.ToArray());

        var entries = workbook.AddSheet("Entries",
            new ExcelColumn("Band"),
            new ExcelColumn("Direction"),
            new ExcelColumn("Name"),
            new ExcelColumn("Detail"),
            new ExcelColumn("Due", ExcelFormat.Date),
            new ExcelColumn("Expected", ExcelFormat.Date),
            new ExcelColumn("Planned week"),
            new ExcelColumn("Moved"),
            new ExcelColumn("Amount", ExcelFormat.Currency));
        foreach (var entry in view.Entries.OrderBy(row => row.Band).ThenBy(row => row.WeekIndex))
        {
            entries.Rows.Add(new object?[]
            {
                BandExportLabel(entry.Band),
                WeeklyCashflowMaths.IsCashIn(entry.Band) ? "In" : "Out",
                entry.Label,
                entry.Detail,
                entry.NaturalDueOn?.UtcDateTime,
                entry.ExpectedOn?.UtcDateTime,
                entry.WeekIndex == view.LaterIndex ? "Later" : $"w/c {view.WeekStarts[entry.WeekIndex].UtcDateTime:dd/MM/yyyy}",
                entry.Moved ? "moved" : "",
                entry.Amount
            });
        }
        return workbook;
    }

    private static string BandExportLabel(WeeklyCashflowBand band) => band switch
    {
        WeeklyCashflowBand.ClientReceipts => "Client invoices",
        WeeklyCashflowBand.SupplierBills => "Supplier bills",
        WeeklyCashflowBand.Subcontractors => "Subcontractors",
        WeeklyCashflowBand.Staff => "Staff",
        WeeklyCashflowBand.Subscriptions => "Subscriptions",
        WeeklyCashflowBand.DirectDebits => "Direct debits",
        _ => "Other"
    };

}
