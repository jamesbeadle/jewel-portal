using Microsoft.JSInterop;

namespace Jewel.JPMS.Services;

/// <summary>
/// Remembers whether a user last viewed mailbox triage oldest-first or newest-first (per browser,
/// per user), so the page reopens in the order they work: oldest-first (the default) clears the
/// backlog from page one; newest-first suits keeping an eye on what's just arrived. Stored value is
/// "oldest" or "newest".
/// </summary>
public sealed class TriageSortStorage
{
    private const string NewestValue = "newest";
    private const string OldestValue = "oldest";

    private const string StorageKeyPrefix = "jpms.triageSort";
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";

    private readonly IJSRuntime js;

    public TriageSortStorage(IJSRuntime js)
    {
        this.js = js;
    }

    public async Task<bool> ReadNewestFirstAsync(string email)
    {
        try { return await js.InvokeAsync<string?>(GetItem, StorageKeyFor(email)) == NewestValue; }
        catch { return false; }
    }

    public async Task WriteAsync(string email, bool newestFirst)
    {
        try { await js.InvokeVoidAsync(SetItem, StorageKeyFor(email), newestFirst ? NewestValue : OldestValue); }
        catch { }
    }

    private static string StorageKeyFor(string email) =>
        $"{StorageKeyPrefix}.{email.Trim().ToLowerInvariant()}";
}
