using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

/// <summary>GET /api/labour/plan/{weekStart} — the week planner for the week any date in it names.</summary>
public sealed record GetLabourWeekPlan(DateTimeOffset WeekStart) : IQuery<LabourWeekPlan>;

/// <summary>POST /api/labour/plan — marks a worker's days as in or not in. Not in records the
/// days as time off asked for (the office's absence record, which the forecast and the chase
/// list already read); in removes whatever absence was recorded. A day already logged cannot be
/// marked not in.</summary>
public sealed record PlanWorkerDays(string WorkerId, IReadOnlyList<DateTimeOffset> Dates, bool IsIn) : ICommand<Acknowledgement>;
