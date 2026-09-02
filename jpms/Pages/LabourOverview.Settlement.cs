using static Jewel.JPMS.Features.Labour.LabourDisplay;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Pages;

public partial class LabourOverview
{
    // ---- Settlement (scope §6, §6a) -------------------------------------------------------------

    private string? expandedScheduleWorkerId;
    private bool isRunningCoding;
    private IReadOnlyList<XeroCodingRunResult>? codingResults;

    private bool settleLineOpen;
    private bool settleLineSaving;
    private string? settleLineError;
    private string settleLineWorkerId = "";
    private string settleLineWorkerName = "";
    private SettlementLineNature settleLineNature = SettlementLineNature.CisMaterials;
    private string settleLineProjectId = "";
    private string settleLineCostCode = "";
    private decimal settleLineAmount;
    private string settleLineNote = "";

    private async Task RunCodingAsync()
    {
        isRunningCoding = true; actionError = null; codingResults = null;
        try { codingResults = await Labour.RunXeroCodingAsync(year, month, null); }
        catch (Exception) { actionError = "The coding run failed — nothing may have reached Xero. Check the error bar and try again."; }
        finally { isRunningCoding = false; }
    }

    private void OpenSettleLine(WorkerSettlementSchedule schedule)
    {
        settleLineWorkerId = schedule.WorkerId;
        settleLineWorkerName = schedule.WorkerName;
        settleLineNature = SettlementLineNature.CisMaterials;
        settleLineProjectId = schedule.Lines.FirstOrDefault()?.ProjectId ?? "";
        settleLineCostCode = schedule.Lines.FirstOrDefault()?.CostCode ?? "";
        settleLineAmount = 0m; settleLineNote = ""; settleLineError = null;
        settleLineOpen = true;
    }

    private async Task SaveSettlementLineAsync()
    {
        settleLineError =
            settleLineAmount <= 0m ? "The amount must be greater than zero."
            : string.IsNullOrWhiteSpace(settleLineProjectId) ? "Say which site the line lands on."
            : string.IsNullOrWhiteSpace(settleLineCostCode) ? "Pick the cost code." : null;
        if (settleLineError is not null) return;
        settleLineSaving = true;
        try
        {
            await Labour.AddSettlementLineAsync(year, month, settleLineWorkerId, settleLineProjectId.Trim(),
                settleLineCostCode.Trim(), settleLineNature, settleLineAmount, settleLineNote);
            settleLineOpen = false;
        }
        catch (Exception) { settleLineError = "Could not add the line — try again."; }
        finally { settleLineSaving = false; }
    }

    private async Task RemoveSettlementLineAsync(string workerSettlementLineId)
    {
        actionError = null;
        try { await Labour.RemoveSettlementLineAsync(year, month, workerSettlementLineId); }
        catch (Exception) { actionError = "Could not remove the line — try again."; }
    }
}
