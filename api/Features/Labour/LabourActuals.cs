using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>What a logged day costs as the office reads it: an approved day carries the cost
/// snapshotted at approval; a day still waiting is valued at the worker's current rate, the same
/// split the Labour overview and the Financials tab draw.</summary>
public static class LabourActuals
{
    public static decimal CostOf(TimesheetEntity timesheet, decimal currentHourlyRate) =>
        timesheet.Status == (int)TimesheetStatus.Approved
            ? timesheet.CostAmount
            : decimal.Round(timesheet.Hours * currentHourlyRate, 2);
}
