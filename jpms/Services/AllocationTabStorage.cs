using Microsoft.JSInterop;

namespace Jewel.JPMS.Services;

/// <summary>
/// Remembers which cost-allocation tab a user was last on (per browser, per
/// user) so the screen reopens where they work: people who look after one
/// project land straight back on that project's queue. Stored value is either
/// a <see cref="Jewel.JPMS.Contracts.Xero.XeroAllocationStatus"/> name or
/// "Project:{projectId}" for a per-project unallocated tab.
/// </summary>
public sealed class AllocationTabStorage
{
    private const string StorageKeyPrefix = "jpms.allocationTab";
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";

    private readonly IJSRuntime js;

    public AllocationTabStorage(IJSRuntime js)
    {
        this.js = js;
    }

    public async Task<string?> ReadAsync(string email)
    {
        try { return await js.InvokeAsync<string?>(GetItem, StorageKeyFor(email)); }
        catch { return null; }
    }

    public async Task WriteAsync(string email, string value)
    {
        try { await js.InvokeVoidAsync(SetItem, StorageKeyFor(email), value); }
        catch { }
    }

    private static string StorageKeyFor(string email) =>
        $"{StorageKeyPrefix}.{email.Trim().ToLowerInvariant()}";
}
