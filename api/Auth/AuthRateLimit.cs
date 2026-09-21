using System.Collections.Concurrent;

namespace Jewel.JPMS.Api.Auth;

/// <summary>
/// How many times one address may knock on the sign-in doors in a window. The account lockout
/// (AuthLockout) answers one address guessing at one account; this answers one address spraying
/// a guess across every account, or filling inboxes through forgot-password. A fixed window per
/// instance — it does not survive a restart and each Functions instance counts alone, which is
/// enough to make a spray slow and a mail-bomb short (security review, 2026-09-21).
/// </summary>
public sealed class AuthRateLimit
{
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    public const int RequestsPerWindow = 30;

    private sealed record Count(DateTimeOffset WindowStartedAt, int Requests);

    private readonly ConcurrentDictionary<string, Count> counts = new(StringComparer.Ordinal);

    public bool IsWithinLimit(string clientKey, DateTimeOffset now)
    {
        var count = counts.AddOrUpdate(clientKey,
            _ => new Count(now, 1),
            (_, existing) => now - existing.WindowStartedAt >= Window ? new Count(now, 1) : existing with { Requests = existing.Requests + 1 });
        if (counts.Count > PruneThreshold) Prune(now);
        return count.Requests <= RequestsPerWindow;
    }

    private const int PruneThreshold = 4096;

    private void Prune(DateTimeOffset now)
    {
        foreach (var pair in counts)
        {
            if (now - pair.Value.WindowStartedAt >= Window) counts.TryRemove(pair.Key, out _);
        }
    }
}
