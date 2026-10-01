namespace Jewel.JPMS.Models;

/// <summary>One worker on one day of the planned week: in by default, not in when the office has
/// recorded an absence for it, and — once the day is logged — what was actually worked.</summary>
public sealed record LabourPlanDay(
    DateTimeOffset Date,
    AbsenceKind? Absence,
    decimal LoggedHours,
    TimesheetStatus? Status,
    decimal ActualCost)
{
    public bool IsIn => Absence is null;
    public bool IsLogged => Status is not null;
}

/// <summary>One worker's planned week against their day rate, with the actuals beside it.</summary>
public sealed record LabourPlanWorker(
    string WorkerId,
    string Name,
    decimal DayRate,
    IReadOnlyList<LabourPlanDay> Days)
{
    public decimal PlannedDays => Days.Sum(WeekPlanRules.PlannedDays);
    public decimal PlannedCost => WeekPlanRules.PlannedCost(DayRate, Days);
    public decimal ActualCost => Days.Sum(day => day.ActualCost);
    public int LoggedDays => Days.Count(day => day.IsLogged);
}

/// <summary>The week planner: every active operative, Monday to Friday, with the forecast cost of
/// the planned days and the actual cost of the days logged so far.</summary>
public sealed record LabourWeekPlan(
    DateTimeOffset WeekStart,
    IReadOnlyList<LabourPlanWorker> Workers)
{
    public decimal PlannedCost => Workers.Sum(worker => worker.PlannedCost);
    public decimal ActualCost => Workers.Sum(worker => worker.ActualCost);
}

/// <summary>The planner's arithmetic: a planned day is a working day with no absence against it,
/// a half day counts half, and the forecast is planned days at the worker's day rate.</summary>
public static class WeekPlanRules
{
    public static decimal PlannedDays(LabourPlanDay day) => day.Absence switch
    {
        null => 1m,
        AbsenceKind.HalfDay => 0.5m,
        _ => 0m,
    };

    public static decimal PlannedCost(decimal dayRate, IEnumerable<LabourPlanDay> days) =>
        decimal.Round(days.Sum(PlannedDays) * dayRate, 2);
}
