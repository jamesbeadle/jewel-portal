using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class LabourOverview
{
    // ---- Export --------------------------------------------------------------------------------

    private ExcelWorkbook? BuildExportWorkbook(bool includeAll)
    {
        var snapshot = Labour.Overview(year, month);
        if (snapshot is null) return null;
        var workbook = new ExcelWorkbook();

        var workers = workbook.AddSheet($"By worker {MonthLabel}",
            new ExcelColumn("Name"), new ExcelColumn("Day rate", ExcelFormat.Currency),
            new ExcelColumn("Contracted days", ExcelFormat.Number), new ExcelColumn("Days worked", ExcelFormat.Number),
            new ExcelColumn("Days off", ExcelFormat.Number), new ExcelColumn("CIS %", ExcelFormat.Number),
            new ExcelColumn("Projected cost", ExcelFormat.Currency), new ExcelColumn("Amount due", ExcelFormat.Currency));
        foreach (var worker in snapshot.Workers)
            workers.AddRow(worker.Name, worker.DayRate, worker.ContractedDays, worker.DaysWorked,
                worker.DaysOff, worker.CisRatePercent, worker.ProjectedCost, worker.AmountDue);

        var sites = workbook.AddSheet("By site",
            new ExcelColumn("Site"), new ExcelColumn("Days", ExcelFormat.Number), new ExcelColumn("Cost", ExcelFormat.Currency));
        foreach (var site in snapshot.Sites) sites.AddRow(site.ProjectName, site.DaysRecorded, site.CostRecorded);

        var codes = workbook.AddSheet("By cost code",
            new ExcelColumn("Cost code"), new ExcelColumn("Trade"), new ExcelColumn("Days", ExcelFormat.Number),
            new ExcelColumn("Cost", ExcelFormat.Currency));
        foreach (var code in snapshot.CostCodes) codes.AddRow(code.CostCode, code.Trade, code.DaysRecorded, code.CostRecorded);

        if (snapshot.Chase.Count > 0)
        {
            var chase = workbook.AddSheet("Chase list",
                new ExcelColumn("Name"), new ExcelColumn("Date", ExcelFormat.Date), new ExcelColumn("Why"));
            foreach (var item in snapshot.Chase)
                chase.AddRow(item.WorkerName, item.Date.UtcDateTime,
                    item.Reason == LabourChaseReason.OpenAttendance ? $"Open sign-in at {item.ProjectName}" : "No timesheet or absence");
        }
        return workbook;
    }
}
