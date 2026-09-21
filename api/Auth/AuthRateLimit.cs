using System.Collections.Concurrent;

namespace Jewel.JPMS.Api.Auth;

/// <summary>
/// How many times one address may knock fruitlessly on a sign-in door in a window. The account
/// lockout (AuthLockout) answers one address guessing at one account; this answers one address
/// spraying a guess across every account, or filling inboxes through forgot-password. A fixed
/// window per instance — it does not survive a restart and each Functions instance counts alone,
/// which is enough to make a spray slow and a mail-bomb short (security review, 2026-09-21).
///
/// Only a knock that achieved nothing is recorded, and each door counts separately. A sign-in
/// that succeeds costs its address nothing, so an office behind one forwarded address cannot
/// lock itself out by working — 46.33.157.82 is one address for every member of staff, which
/// made the first version of this a staff lockout rather than a control (correction, 2026-09-21).
/// </summary>
public sealed class AuthRateLimit
{
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    public const int RequestsPerWindow = 30;

    private const int PruneThreshold = 4096;

    private sealed record Count(DateTimeOffset WindowStartedAt, int Requests);

    private readonly ConcurrentDictionary<string, Count> counts = new(StringComparer.Ordinal);

    public bool IsWithinLimit(string doorKey, DateTimeOffset now)
    {
        if (!counts.TryGetValue(doorKey, out var count)) return true;
        var hasWindowExpired = now - count.WindowStartedAt >= Window;
        return hasWindowExpired || count.Requests < RequestsPerWindow;
    }

    public void RecordKnock(string doorKey, DateTimeOffset now)
    {
        counts.AddOrUpdate(doorKey,
            _ => new Count(now, 1),
            (_, existing) => now - existing.WindowStartedAt >= Window
                ? new Count(now, 1)
                : existing with { Requests = existing.Requests + 1 });
        if (counts.Count > PruneThreshold) Prune(now);
    }

    private void Prune(DateTimeOffset now)
    {
        foreach (var pair in counts)
        {
            if (now - pair.Value.WindowStartedAt >= Window) counts.TryRemove(pair.Key, out _);
        }
    }
}
