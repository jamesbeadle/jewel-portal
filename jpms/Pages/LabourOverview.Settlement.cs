using Jewel.JPMS.Features.Labour;
using static Jewel.JPMS.Features.Labour.LabourDisplay;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Pages;

public partial class LabourOverview
{
    // ---- Settlement (scope §6, §6a) -------------------------------------------------------------

    private string? expandedScheduleWorkerId;
    private bool isRunningCoding;
    private IReadOnlyList<XeroCodingRunResult>? codingResults;

    private async Task RunCodingAsync()
    {
        isRunningCoding = true; actionError = null; codingResults = null;
        try { codingResults = await Labour.RunXeroCodingAsync(year, month, null); }
        catch (Exception) { actionError = "The coding run failed — nothing may have reached Xero. Check the error bar and try again."; }
        finally { isRunningCoding = false; }
    }

    private void OpenSettleLine(WorkerSettlementSchedule schedule) => settlementLineModal!.Open(schedule);

    private async Task RemoveSettlementLineAsync(string workerSettlementLineId)
    {
        actionError = null;
        try { await Labour.RemoveSettlementLineAsync(year, month, workerSettlementLineId); }
        catch (Exception) { actionError = "Could not remove the line — try again."; }
    }
}
