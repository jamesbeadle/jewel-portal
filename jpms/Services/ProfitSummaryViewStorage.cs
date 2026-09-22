namespace Jewel.JPMS.Services;

/// <summary>
/// Remembers how a user last read the Profit Summary (per browser, per user, like
/// <see cref="WorkOrderGroupingStorage"/>): whether the profit table was compact, so a reader
/// who fits the table to a laptop screen finds it fitted next time.
/// </summary>
public sealed class ProfitSummaryViewStorage
{
    private const string CompactKeyPrefix = "jpms.profitSummary.compact";
    private const string TrueValue = "true";
    private const string FalseValue = "false";
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";

    private readonly IJSRuntime js;

    public ProfitSummaryViewStorage(IJSRuntime js)
    {
        this.js = js;
    }

    public async Task<bool> ReadAsync(string email)
    {
        try { return await js.InvokeAsync<string?>(GetItem, KeyFor(CompactKeyPrefix, email)) == TrueValue; }
        catch { return false; }
    }

    public async Task WriteAsync(string email, bool isCompact)
    {
        try { await js.InvokeVoidAsync(SetItem, KeyFor(CompactKeyPrefix, email), isCompact ? TrueValue : FalseValue); }
        catch { }
    }

    private static string KeyFor(string prefix, string email) =>
        $"{prefix}.{email.Trim().ToLowerInvariant()}";
}
