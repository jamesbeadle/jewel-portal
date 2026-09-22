namespace Jewel.JPMS.Services;

/// <summary>How a user last read the Profit Summary: whether the profit table was compact, and
/// how many months the running-profit grid showed.</summary>
public sealed record ProfitSummaryView(bool IsCompactTable, int RunningWindowMonths)
{
    public const int DefaultRunningWindowMonths = 6;
    public static readonly IReadOnlyList<int> RunningWindowChoices = new[] { 6, 9, 12 };
    public static readonly ProfitSummaryView Default = new(false, DefaultRunningWindowMonths);
}

/// <summary>
/// Remembers a user's Profit Summary view between visits (per browser, per user, like
/// <see cref="WorkOrderGroupingStorage"/>), so a reader who fits the table to a laptop screen
/// or widens the grid to a year finds it so next time.
/// </summary>
public sealed class ProfitSummaryViewStorage
{
    private const string CompactKeyPrefix = "jpms.profitSummary.compact";
    private const string WindowKeyPrefix = "jpms.profitSummary.runningWindowMonths";
    private const string TrueValue = "true";
    private const string FalseValue = "false";
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";

    private readonly IJSRuntime js;

    public ProfitSummaryViewStorage(IJSRuntime js)
    {
        this.js = js;
    }

    public async Task<ProfitSummaryView> ReadAsync(string email)
    {
        try
        {
            var isCompact = await js.InvokeAsync<string?>(GetItem, KeyFor(CompactKeyPrefix, email)) == TrueValue;
            var storedMonths = await js.InvokeAsync<string?>(GetItem, KeyFor(WindowKeyPrefix, email));
            return new ProfitSummaryView(isCompact, WindowMonthsFrom(storedMonths));
        }
        catch { return ProfitSummaryView.Default; }
    }

    public async Task WriteAsync(string email, ProfitSummaryView view)
    {
        try
        {
            await js.InvokeVoidAsync(SetItem, KeyFor(CompactKeyPrefix, email), view.IsCompactTable ? TrueValue : FalseValue);
            await js.InvokeVoidAsync(SetItem, KeyFor(WindowKeyPrefix, email), view.RunningWindowMonths.ToString());
        }
        catch { }
    }

    // A stored length that is no longer one of the choices falls back to the default.
    private static int WindowMonthsFrom(string? stored) =>
        int.TryParse(stored, out var months) && ProfitSummaryView.RunningWindowChoices.Contains(months)
            ? months
            : ProfitSummaryView.DefaultRunningWindowMonths;

    private static string KeyFor(string prefix, string email) =>
        $"{prefix}.{email.Trim().ToLowerInvariant()}";
}
