namespace Jewel.JPMS.Models;

// One task that has slipped against the baseline. SlipDays is end-date movement in calendar days
// (positive = later than baselined). DrivesCompletion marks the event(s) pushing the project's
// overall completion — the ones that matter contractually (JCT ICD 2024 cl. 2.19: notice when
// progress is being or is likely to be delayed).
public sealed record ProgrammeDelayEvent(
    string ProgrammeTaskId,
    string Title,
    DateTimeOffset BaselineStart,
    DateTimeOffset BaselineEnd,
    DateTimeOffset PlannedStart,
    DateTimeOffset PlannedEnd,
    int SlipDays,
    bool DrivesCompletion);

// The movement of the programme against its latest baseline. CompletionSlipDays compares the
// latest planned end across all tasks with the latest baselined end; DelayEvents lists every task
// whose end has moved later. Null baseline fields mean no baseline exists yet, so movement cannot
// be measured (the first thing a well-run programme should do is take one).
public sealed record ProgrammeMovement(
    DateTimeOffset? BaselineCompletion,
    DateTimeOffset? CurrentCompletion,
    int CompletionSlipDays,
    IReadOnlyList<ProgrammeDelayEvent> DelayEvents)
{
    public bool HasSlippage => DelayEvents.Count > 0;
}

// Pure comparison of the live programme against a baseline snapshot. Shared by the API (the
// Programme Agent's delay analysis) and the Blazor Programme tab (the movement banner), so both
// always agree on what "the programme has moved" means. No I/O, no clock — trivially testable.
public static class ProgrammeMovementCalculator
{
    public static ProgrammeMovement Compare(
        IReadOnlyList<ProgrammeTask> tasks,
        IReadOnlyList<ProgrammeBaselineTask> baselineTasks)
    {
        var baselineByTaskId = baselineTasks.ToDictionary(b => b.ProgrammeTaskId);

        DateTimeOffset? currentCompletion = tasks.Count > 0 ? tasks.Max(t => t.PlannedEnd) : null;
        DateTimeOffset? baselineCompletion = baselineTasks.Count > 0 ? baselineTasks.Max(b => b.PlannedEnd) : null;

        var completionSlipDays = currentCompletion is { } current && baselineCompletion is { } baseline
            ? (int)Math.Round((current - baseline).TotalDays)
            : 0;

        var events = new List<ProgrammeDelayEvent>();
        foreach (var task in tasks)
        {
            if (!baselineByTaskId.TryGetValue(task.ProgrammeTaskId, out var baselined)) continue;

            var slipDays = (int)Math.Round((task.PlannedEnd - baselined.PlannedEnd).TotalDays);
            if (slipDays <= 0) continue;

            // The event drives completion when this task's new end lands on (or defines) the
            // project's latest planned end — i.e. removing its slip would pull completion back.
            var drivesCompletion = currentCompletion is { } completion && task.PlannedEnd == completion && completionSlipDays > 0;

            events.Add(new ProgrammeDelayEvent(
                task.ProgrammeTaskId,
                task.Title,
                baselined.PlannedStart,
                baselined.PlannedEnd,
                task.PlannedStart,
                task.PlannedEnd,
                slipDays,
                drivesCompletion));
        }

        return new ProgrammeMovement(
            baselineCompletion,
            currentCompletion,
            completionSlipDays,
            events.OrderByDescending(e => e.SlipDays).ToList().AsReadOnly());
    }
}
