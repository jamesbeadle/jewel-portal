using static Jewel.JPMS.MoneyFormats;
using Jewel.JPMS.Services.Excel;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Pages;

public partial class ProjectLabour
{
    // ---- Settlement cover sub-view --------------------------------------------------------------

    private bool showCoverPanel;
    private IReadOnlyList<Jewel.JPMS.Contracts.Xero.XeroLedgerLine>? ledgerLines;
    private readonly Dictionary<string, string> coverSubcontractorByLine = new();

    // The hundred most recent allocated lines on THIS project.
    private const int CoverableLedgerLines = 100;

    private async Task LoadLedgerAsync()
    {
        showCoverPanel = true;
        try
        {
            ledgerLines = await Queries.AskAsync(
                new Jewel.JPMS.Contracts.Xero.ListXeroLedgerLinesForProject(ProjectId, CoverableLedgerLines),
                CancellationToken.None);
        }
        catch (Exception) { actionError = "Could not load the Xero ledger — try again."; showCoverPanel = false; }
    }

    // Already filtered, ordered and capped by the server — this just unwraps the nullable.
    private List<Jewel.JPMS.Contracts.Xero.XeroLedgerLine> ProjectLedgerLines() =>
        (ledgerLines ?? Array.Empty<Jewel.JPMS.Contracts.Xero.XeroLedgerLine>()).ToList();

    private List<(string Id, string Name)> WorkerSubcontractors() =>
        Labour.SettlementFor(ProjectId)
            .Where(row => row.SubcontractorId != "")
            .Select(row => (row.SubcontractorId, row.SubcontractorName))
            .ToList();

    private async Task MarkCoveredAsync(Jewel.JPMS.Contracts.Xero.XeroLedgerLine line)
    {
        if (!coverSubcontractorByLine.TryGetValue(line.XeroLedgerLineId, out var subcontractorId) || subcontractorId == "")
        {
            actionError = "Pick which subcontractor's timesheets the invoice line settles first.";
            return;
        }
        actionError = null;
        var lineDate = line.Date ?? DateTime.Today;
        var periodStart = new DateTimeOffset(new DateTime(lineDate.Year, lineDate.Month, 1), TimeSpan.Zero);
        try
        {
            await Labour.SetTimesheetCoverAsync(ProjectId, line.XeroLedgerLineId, true, subcontractorId, periodStart, periodStart.AddMonths(1));
            ledgerLines = ledgerLines!.Where(candidate => candidate.XeroLedgerLineId != line.XeroLedgerLineId).ToList();
        }
        catch (Exception failure) { ReportFailure(failure, "Could not mark the line as covered — try again."); }
    }

    private string assignWorkerId = "";

    private List<Worker> UnassignedWorkers()
    {
        var assigned = Labour.AssignmentsFor(ProjectId)
            .Where(assignment => assignment.IsActive)
            .Select(assignment => assignment.WorkerId)
            .ToHashSet();
        return Labour.Workers()
            .Where(worker => worker.IsActive && !assigned.Contains(worker.WorkerId))
            .ToList();
    }

    private async Task AssignWorkerAsync()
    {
        if (assignWorkerId == "") { actionError = "Pick a worker to assign first."; return; }
        actionError = null;
        try
        {
            await Labour.SetAssignmentAsync(ProjectId, assignWorkerId, true);
            assignWorkerId = "";
        }
        catch (Exception failure) { ReportFailure(failure, "Could not assign the worker — try again."); }
    }

    private async Task UnassignAsync(string workerId)
    {
        actionError = null;
        try { await Labour.SetAssignmentAsync(ProjectId, workerId, false); }
        catch (Exception failure) { ReportFailure(failure, "Could not remove the worker — try again."); }
    }

    // -- Excel export -----------------------------------------------------------

    private bool HasExportableLabourData() =>
        WeekTimesheets().Count > 0
        || Labour.AttendanceFor(ProjectId).Count > 0
        || Labour.SettlementFor(ProjectId).Count > 0;

    private static string TimesheetStatusLabel(TimesheetStatus status) => status switch
    {
        TimesheetStatus.Approved => "Approved",
        TimesheetStatus.Rejected => "Rejected",
        _ => "Submitted"
    };

    private ExcelWorkbook? BuildExportWorkbook(bool includeAllRows)
    {
        if (!HasExportableLabourData()) return null;

        var workbook = new ExcelWorkbook();

        var timesheetSheet = workbook.AddSheet("Timesheets",
            new ExcelColumn("Worker"),
            new ExcelColumn("Day", ExcelFormat.Date),
            new ExcelColumn("Cost code"),
            new ExcelColumn("Hours", ExcelFormat.Number),
            new ExcelColumn("£", ExcelFormat.Currency),
            new ExcelColumn("Status"));
        foreach (var timesheet in WeekTimesheets())
        {
            timesheetSheet.AddRow(
                timesheet.WorkerName,
                timesheet.WorkedOn.LocalDateTime,
                timesheet.CostCode,
                timesheet.Hours,
                timesheet.Status == TimesheetStatus.Approved ? (decimal?)timesheet.CostAmount : null,
                TimesheetStatusLabel(timesheet.Status));
        }

        var registerSheet = workbook.AddSheet("Site register",
            new ExcelColumn("Date", ExcelFormat.Date),
            new ExcelColumn("Worker"),
            new ExcelColumn("In"),
            new ExcelColumn("Out"));
        var registerRows = includeAllRows
            ? Labour.AttendanceFor(ProjectId)
            : Labour.AttendanceFor(ProjectId).Take(60).ToList();
        foreach (var row in registerRows)
        {
            registerSheet.AddRow(
                row.WorkDate.LocalDateTime,
                row.WorkerName,
                row.SignedInAt.ToLocalTime().ToString("HH:mm"),
                row.SignedOutAt?.ToLocalTime().ToString("HH:mm"));
        }

        var settlement = Labour.SettlementFor(ProjectId);
        if (settlement.Count > 0)
        {
            var settlementSheet = workbook.AddSheet("Settlement",
                new ExcelColumn("Subcontractor"),
                new ExcelColumn("Approved £", ExcelFormat.Currency),
                new ExcelColumn("Covered invoices £", ExcelFormat.Currency),
                new ExcelColumn("Posted variances £", ExcelFormat.Currency),
                new ExcelColumn("Unresolved £", ExcelFormat.Currency));
            foreach (var row in settlement)
            {
                settlementSheet.AddRow(
                    row.SubcontractorName,
                    row.ApprovedCost,
                    row.CoveredInvoiceTotal,
                    row.PostedVarianceTotal,
                    row.UnresolvedVariance);
            }
        }

        return workbook;
    }
}
