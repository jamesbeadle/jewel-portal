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
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Pages;

public partial class ProjectLabour
{
    // ---- Person-by-person approval (2026-08-28, Jeremy's ask): filter the grid to one
    //      worker, select all, bulk-code, approve. The filter narrows what the header
    //      checkbox and the footer buttons act on; changing it clears the selection so a
    //      hidden row can never be coded or approved unseen. ----

    private string workerFilter = "";
    private string bulkCostCode = "";
    private bool isBulkCoding;

    private void SetWorkerFilter(string? value)
    {
        workerFilter = value ?? "";
        selectedIds.Clear();
    }

    private List<TimesheetDetail> FilteredWeek(List<TimesheetDetail> week) =>
        workerFilter == "" ? week : week.Where(timesheet => timesheet.WorkerName == workerFilter).ToList();

    private static List<string> WeekWorkerNames(List<TimesheetDetail> week) =>
        week.Select(timesheet => timesheet.WorkerName).Distinct().OrderBy(name => name).ToList();

    /// <summary>
    /// Apply one cost code to every selected submitted row — the bulk twin of Adjust, sharing
    /// its endpoint (and so its rules: submitted rows only; approved rows are immutable).
    /// Hours are passed through unchanged. The selection is kept so Approve selected follows on.
    /// </summary>
    private async Task CodeSelectedAsync()
    {
        if (bulkCostCode == "") { actionError = "Pick the cost code to apply to the selected rows first."; return; }
        actionError = null;
        approvalFailures = Array.Empty<LabourApprovalFailure>();
        isBulkCoding = true;
        var problems = new List<string>();
        try
        {
            var targets = Labour.TimesheetsFor(ProjectId)
                .Where(timesheet => selectedIds.Contains(timesheet.TimesheetId)
                                    && timesheet.Status == TimesheetStatus.Submitted)
                .OrderBy(timesheet => timesheet.WorkedOn)
                .ToList();
            foreach (var timesheet in targets)
            {
                try
                {
                    await Labour.AdjustTimesheetAsync(ProjectId, timesheet.TimesheetId, timesheet.Hours, bulkCostCode);
                }
                catch (Exception failure)
                {
                    problems.Add($"{timesheet.WorkerName} {timesheet.WorkedOn:ddd dd MMM}: {DescribeFailure(failure, "could not be coded")}");
                }
            }
        }
        finally { isBulkCoding = false; }
        if (problems.Count > 0)
            actionError = "Some rows could not be coded — " + string.Join("; ", problems);
    }

    private List<TimesheetDetail> WeekTimesheets() =>
        Labour.TimesheetsFor(ProjectId)
            .Where(timesheet => timesheet.WorkedOn >= weekStart && timesheet.WorkedOn < weekStart.AddDays(7))
            .OrderBy(timesheet => timesheet.WorkedOn).ThenBy(timesheet => timesheet.WorkerName)
            .ToList();

    private bool AllSubmittedSelected(List<TimesheetDetail> week) =>
        week.Any(timesheet => timesheet.Status == TimesheetStatus.Submitted)
        && week.Where(timesheet => timesheet.Status == TimesheetStatus.Submitted)
               .All(timesheet => selectedIds.Contains(timesheet.TimesheetId));

    private void ToggleAll(List<TimesheetDetail> week, bool selected)
    {
        foreach (var timesheet in week.Where(timesheet => timesheet.Status == TimesheetStatus.Submitted))
        {
            if (selected) selectedIds.Add(timesheet.TimesheetId);
            else selectedIds.Remove(timesheet.TimesheetId);
        }
    }

    private void ToggleSelected(string timesheetId, bool selected)
    {
        if (selected) selectedIds.Add(timesheetId);
        else selectedIds.Remove(timesheetId);
    }

    private async Task ApproveSelectedAsync()
    {
        actionError = null;
        approvalFailures = Array.Empty<LabourApprovalFailure>();
        isApproving = true;
        try
        {
            var result = await Labour.ApproveTimesheetsAsync(ProjectId, selectedIds.ToList());
            approvalFailures = result.Failures;
            selectedIds.Clear();
        }
        catch (Exception failure)
        {
            ReportFailure(failure, "Approval failed — check your connection and try again.");
        }
        finally { isApproving = false; }
    }

    private void OpenOverBudget(IReadOnlyList<LabourApprovalFailure> budgetBlocked)
    {
        overBudgetFailures = budgetBlocked;
        overBudgetReason = ""; overBudgetError = null;
        overBudgetOpen = true;
    }

    private void CloseOverBudget()
    {
        overBudgetOpen = false;
        overBudgetFailures = Array.Empty<LabourApprovalFailure>();
    }

    private async Task ConfirmOverBudgetAsync()
    {
        if (string.IsNullOrWhiteSpace(overBudgetReason)) return;
        isOverriding = true; overBudgetError = null;
        try
        {
            // Re-send ONLY the budget-blocked ids with the override flag — anything else that
            // failed keeps its failure, and anything already approved never rides along.
            var result = await Labour.ApproveTimesheetsAsync(ProjectId,
                overBudgetFailures.Select(failure => failure.TimesheetId).ToList(),
                allowOverBudget: true, overBudgetReason: overBudgetReason.Trim());
            approvalFailures = result.Failures;
            CloseOverBudget();
        }
        catch (Exception failure)
        {
            overBudgetError = DescribeFailure(failure,
                "Could not approve over budget — check your connection and try again.");
        }
        finally { isOverriding = false; }
    }

    private void StartEdit(TimesheetDetail timesheet)
    {
        editingId = timesheet.TimesheetId;
        editHours = timesheet.Hours;
        editCostCode = timesheet.CostCode;
    }

    private async Task SaveEditAsync()
    {
        if (editingId is null) return;
        actionError = editCostCode == "" ? "Pick a cost code." : HoursProblem(editHours);
        if (actionError is not null) return;
        try
        {
            await Labour.AdjustTimesheetAsync(ProjectId, editingId, editHours, editCostCode);
            editingId = null;
        }
        catch (Exception failure)
        {
            ReportFailure(failure, "Could not save the adjustment — check your connection and try again.");
        }
    }

    private void StartReject(TimesheetDetail timesheet)
    {
        rejectingId = timesheet.TimesheetId;
        rejectReason = "";
        rejectError = null;
    }

    private async Task ConfirmRejectAsync()
    {
        if (rejectingId is null) return;
        if (string.IsNullOrWhiteSpace(rejectReason))
        {
            rejectError = "Give a reason — the worker sees it, and it's how they know what to fix.";
            return;
        }
        rejectError = null;
        try
        {
            await Labour.RejectTimesheetAsync(ProjectId, rejectingId, rejectReason);
            rejectingId = null;
        }
        catch (Exception failure)
        {
            rejectError = DescribeFailure(failure, "Could not reject the timesheet — check your connection and try again.");
        }
    }

}
