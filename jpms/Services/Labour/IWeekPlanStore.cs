namespace Jewel.JPMS.Services;

/// <summary>The week planner: who is in each day, and the one tap that changes it.</summary>
public interface IWeekPlanStore
{
    event Action? OnChange;

    LabourWeekPlan? Plan(DateTimeOffset weekStart);
    Task RefreshAsync(DateTimeOffset weekStart);
    /// <summary>Marks the days in or not in and re-reads the week.</summary>
    Task PlanDaysAsync(DateTimeOffset weekStart, string workerId, IReadOnlyList<DateTimeOffset> dates, bool isIn);
}
