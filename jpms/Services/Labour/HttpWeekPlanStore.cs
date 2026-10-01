using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Services;

public sealed class HttpWeekPlanStore : IWeekPlanStore
{
    private const string WeekKeyFormat = "yyyy-MM-dd";
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;
    private readonly Dictionary<string, LabourWeekPlan> plans = new();

    public HttpWeekPlanStore(IQueryClient queries, ICommandSender commands) { this.queries = queries; this.commands = commands; }

    public event Action? OnChange;

    public LabourWeekPlan? Plan(DateTimeOffset weekStart) => plans.GetValueOrDefault(WeekKey(weekStart));

    public async Task RefreshAsync(DateTimeOffset weekStart)
    {
        var plan = await queries.AskAsync(new GetLabourWeekPlan(weekStart), CancellationToken.None);
        plans[WeekKey(plan.WeekStart)] = plan;
        OnChange?.Invoke();
    }

    public async Task PlanDaysAsync(DateTimeOffset weekStart, string workerId, IReadOnlyList<DateTimeOffset> dates, bool isIn)
    {
        await commands.SendAsync(new PlanWorkerDays(workerId, dates, isIn), CancellationToken.None);
        await RefreshAsync(weekStart);
    }

    private static string WeekKey(DateTimeOffset weekStart) => weekStart.ToString(WeekKeyFormat);
}
