namespace Jewel.JPMS.Services;

/// <summary>
/// Backs a synchronous read accessor with an async query, without ever blocking the caller —
/// blocking on async (e.g. <c>.GetAwaiter().GetResult()</c>) deadlocks on Blazor WebAssembly.
///
/// The first read of a key returns the supplied fallback and starts a background fetch; when it
/// lands, <c>onChanged</c> fires so subscribed views re-render and read the now-cached value.
/// A key is fetched at most once until <see cref="Invalidate"/> is called, so a legitimately
/// empty result cannot drive a render → fetch → render loop.
///
/// Blazor WebAssembly is single-threaded, so no locking is required.
/// </summary>
public sealed class AsyncQueryCache<TKey, TValue> where TKey : notnull
{
    private readonly Func<TKey, CancellationToken, Task<TValue>> fetch;
    private readonly Action onChanged;
    private readonly Dictionary<TKey, TValue> values = new();
    private readonly HashSet<TKey> requested = new();

    public AsyncQueryCache(Func<TKey, CancellationToken, Task<TValue>> fetch, Action onChanged)
    {
        this.fetch = fetch;
        this.onChanged = onChanged;
    }

    /// <summary>Returns the cached value for a key, starting a one-time background load on first use.</summary>
    public TValue Get(TKey key, TValue fallback)
    {
        EnsureLoaded(key);
        return values.TryGetValue(key, out var value) ? value : fallback;
    }

    /// <summary>Starts a background load for a key if one has not already been started.</summary>
    public void EnsureLoaded(TKey key)
    {
        // Add returns false when the key is already present, which also guards against
        // duplicate in-flight fetches.
        if (!requested.Add(key)) return;
        _ = LoadAsync(key);
    }

    /// <summary>
    /// Drops the cached value for a key and refetches it, firing <c>onChanged</c> when fresh
    /// data lands. Call after a mutation that affects this key.
    /// </summary>
    public void Invalidate(TKey key)
    {
        values.Remove(key);
        requested.Remove(key);
        EnsureLoaded(key);
    }

    /// <summary>Drops every cached value and lets the next read refetch. Use when a mutation's
    /// impact spans keys that aren't known at the call site.</summary>
    public void InvalidateAll()
    {
        values.Clear();
        requested.Clear();
        onChanged();
    }

    private async Task LoadAsync(TKey key)
    {
        try
        {
            values[key] = await fetch(key, CancellationToken.None);
            onChanged();
        }
        catch
        {
            // Allow a later retry; the failed fetch raised no onChanged, so it cannot loop.
            requested.Remove(key);
        }
    }
}
