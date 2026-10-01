using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Services;

public sealed class HttpMyWeeksStore : IMyWeeksStore
{
    private const string WeekKeyFormat = "yyyy-MM-dd";
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;
    private readonly Dictionary<string, MyLabourWeek> weeks = new();
    private readonly Dictionary<string, MyLabourMonth> months = new();

    public HttpMyWeeksStore(IQueryClient queries, ICommandSender commands) { this.queries = queries; this.commands = commands; }

    public event Action? OnChange;

    public MyLabourWeek? Week(DateTimeOffset weekStart) => weeks.GetValueOrDefault(WeekKey(weekStart));

    public async Task RefreshWeekAsync(DateTimeOffset weekStart)
    {
        var week = await queries.AskAsync(new GetMyLabourWeek(weekStart), CancellationToken.None);
        Keep(week);
    }

    public MyLabourMonth? Month(int year, int month) => months.GetValueOrDefault(MonthKey(year, month));

    public async Task RefreshMonthAsync(int year, int month)
    {
        months[MonthKey(year, month)] = await queries.AskAsync(new GetMyLabourMonth(year, month), CancellationToken.None);
        OnChange?.Invoke();
    }

    public async Task<MyLabourWeek> SubmitWeekAsync(DateTimeOffset weekStart)
    {
        var week = await commands.SendAsync(new MySubmitWeek(weekStart), CancellationToken.None);
        Keep(week);
        return week;
    }

    private void Keep(MyLabourWeek week)
    {
        weeks[WeekKey(week.WeekStart)] = week;
        OnChange?.Invoke();
    }

    private static string WeekKey(DateTimeOffset weekStart) => weekStart.ToString(WeekKeyFormat);

    private static string MonthKey(int year, int month) => $"{year}-{month}";
}
