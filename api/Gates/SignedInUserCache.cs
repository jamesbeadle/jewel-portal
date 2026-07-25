using System.Collections.Concurrent;

namespace Jewel.JPMS.Api.Gates;

/// <summary>
/// A short-lived, in-process cache of the resolved caller, keyed by the hashed session cookie.
///
/// Why this exists: resolving the caller used to cost three sequential database round-trips
/// (session → directory user → role list) on EVERY authenticated endpoint — roughly 340 call sites
/// — before a single byte of business data was touched. On a serverless SQL tier that fixed cost
/// dominated small requests and stacked up on pages that fire several calls at once.
///
/// The trade-off is bounded staleness: a permission change can take up to <see cref="Ttl"/> to be
/// seen by a warm instance that has already cached the caller. The three places that actually
/// change permissions (directory upsert, directory removal, invite) call
/// <see cref="InvalidateEmail"/>, and logout calls <see cref="RemoveSession"/>, so in practice the
/// window only applies to changes made out-of-band (direct SQL, or another running instance).
/// Keep the TTL short for that reason — this is a latency cache, not a session store.
///
/// Registered as a singleton, so each Functions instance keeps its own copy; there is no
/// cross-instance invalidation and none is wanted, the TTL is the backstop.
/// </summary>
public sealed class SignedInUserCache
{
    /// <summary>How long a resolved caller may be reused before it is re-read from the database.</summary>
    public static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    /// <summary>Above this many live entries, a Set call prunes expired ones first. Sessions are
    /// per-user-per-device, so the table stays small; this is only a runaway guard.</summary>
    private const int PruneThreshold = 512;

    private sealed record Entry(SignedInUser User, DateTimeOffset ResolvedAt, DateTimeOffset SessionExpiresAt);

    private readonly ConcurrentDictionary<string, Entry> entries = new(StringComparer.Ordinal);

    /// <summary>The resolved caller for this session id, or null if absent, stale, or the
    /// underlying session has since expired.</summary>
    public SignedInUser? Get(string sessionId, DateTimeOffset now)
    {
        if (!entries.TryGetValue(sessionId, out var entry)) return null;
        if (now - entry.ResolvedAt >= Ttl || entry.SessionExpiresAt <= now)
        {
            entries.TryRemove(sessionId, out _);
            return null;
        }
        return entry.User;
    }

    /// <summary>Caches a freshly resolved caller. <paramref name="sessionExpiresAt"/> is the
    /// session row's own expiry, so a cached entry can never outlive the session it came from.</summary>
    public void Set(string sessionId, SignedInUser user, DateTimeOffset sessionExpiresAt, DateTimeOffset now)
    {
        if (entries.Count >= PruneThreshold) Prune(now);
        entries[sessionId] = new Entry(user, now, sessionExpiresAt);
    }

    /// <summary>Drops one session — call on logout / session revocation.</summary>
    public void RemoveSession(string sessionId) => entries.TryRemove(sessionId, out _);

    /// <summary>Drops every cached session for one user — call whenever their directory record or
    /// role list changes, so the new permissions take effect on the next request rather than after
    /// the TTL.</summary>
    public void InvalidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return;
        foreach (var pair in entries)
        {
            if (string.Equals(pair.Value.User.Email, email, StringComparison.OrdinalIgnoreCase))
                entries.TryRemove(pair.Key, out _);
        }
    }

    private void Prune(DateTimeOffset now)
    {
        foreach (var pair in entries)
        {
            var entry = pair.Value;
            if (now - entry.ResolvedAt >= Ttl || entry.SessionExpiresAt <= now)
                entries.TryRemove(pair.Key, out _);
        }
    }
}
