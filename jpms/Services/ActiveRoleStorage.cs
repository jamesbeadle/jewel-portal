using System.Globalization;

namespace Jewel.JPMS.Services;

/// <summary>
/// Persists the "Viewing as" choice per user in localStorage, alongside WHEN it was made.
/// The timestamp is what lets a role switch be temporary (SessionService's two-hour default-back
/// for opted-in users): a bare role with no timestamp — every value written before the timestamp
/// existed — reads as "switched long ago", which is exactly right, because those are the stuck
/// Administrator views this feature was built to unstick.
/// </summary>
public sealed class ActiveRoleStorage
{
    private const string StorageKeyPrefix = "jpms.activeRole";
    private const string SwitchedAtKeySuffix = ".switchedAt";
    private const string GetItem = "localStorage.getItem";
    private const string SetItem = "localStorage.setItem";
    private const string RemoveItem = "localStorage.removeItem";

    private readonly IJSRuntime js;

    public ActiveRoleStorage(IJSRuntime js)
    {
        this.js = js;
    }

    public sealed record StoredRole(Role Role, DateTimeOffset? SwitchedAt);

    public async Task<StoredRole?> ReadAsync(string email)
    {
        var stored = await TryGetItem(StorageKeyFor(email));
        if (string.IsNullOrWhiteSpace(stored)) return null;
        if (!Enum.TryParse<Role>(stored, out var role)) return null;

        var switchedAtRaw = await TryGetItem(StorageKeyFor(email) + SwitchedAtKeySuffix);
        DateTimeOffset? switchedAt =
            DateTimeOffset.TryParse(switchedAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed
                : null;
        return new StoredRole(role, switchedAt);
    }

    /// <summary>Stores the active role; <paramref name="switchedAt"/> is when the user moved OFF
    /// their own role (null when this IS their own role, or when reverting is off for them —
    /// nothing to time in either case).</summary>
    public async Task WriteAsync(string email, Role role, DateTimeOffset? switchedAt = null)
    {
        await TrySetItem(StorageKeyFor(email), role.ToString());
        var switchedAtKey = StorageKeyFor(email) + SwitchedAtKeySuffix;
        if (switchedAt is { } at) await TrySetItem(switchedAtKey, at.ToString("o", CultureInfo.InvariantCulture));
        else await TryRemoveItem(switchedAtKey);
    }

    private async Task<string?> TryGetItem(string key)
    {
        try { return await js.InvokeAsync<string?>(GetItem, key); }
        catch { return null; }
    }

    private async Task TrySetItem(string key, string value)
    {
        try { await js.InvokeVoidAsync(SetItem, key, value); }
        catch { }
    }

    private async Task TryRemoveItem(string key)
    {
        try { await js.InvokeVoidAsync(RemoveItem, key); }
        catch { }
    }

    private static string StorageKeyFor(string email) =>
        $"{StorageKeyPrefix}.{email.Trim().ToLowerInvariant()}";
}
