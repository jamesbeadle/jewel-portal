
namespace Jewel.JPMS.Api.Features.Platform;

/// <summary>
/// The announced version, cached per functions instance so VersionStampMiddleware can stamp every
/// response without a database read per request. Reads through at most once per minute (successful
/// or not, so a down database costs one slow request a minute, not one per request). A publish on
/// THIS instance updates the cache immediately (PublishAppVersionHandler calls Update); other
/// instances catch up within the TTL — a minute of skew is fine by design, because every
/// subsequent response repeats the answer and the tab-focus check reads the database directly.
/// </summary>
public sealed class AnnouncedVersionCache
{
    /// <summary>The one row's key — the table is a single row by design.</summary>
    public const string CurrentRowId = "current";

    // Short on purpose: a tab that refreshes right after a publish baselines on the first header
    // it sees, and a header from an instance still holding the OLD number would re-prompt it when
    // a caught-up instance answers next. The window can't be zero without a read per request —
    // 15 seconds keeps it small at the cost of one single-row PK read per instance per 15s.
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(15);

    private readonly object gate = new();
    private long? version;
    private DateTimeOffset lastAttemptAt;

    /// <summary>The cached version, read through from the database when stale. Null when no read
    /// has ever succeeded (schema behind the code, or the database unreachable since start-up) —
    /// callers fall back to BuildVersion and the next stale request tries again.</summary>
    public async Task<long?> GetAsync(JpmsContext context, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            if (DateTimeOffset.UtcNow - lastAttemptAt < Ttl) return version;
            lastAttemptAt = DateTimeOffset.UtcNow;
        }

        try
        {
            var row = await context.AppVersions.AsNoTracking()
                .FirstOrDefaultAsync(current => current.AppVersionId == CurrentRowId, cancellationToken);
            if (row is not null) Update(row.Version);
        }
        catch
        {
            // Stamping the version is a courtesy, not the request's job — a missing table or an
            // unreachable database must never take the response down with it.
        }

        lock (gate) return version;
    }

    /// <summary>Record a version known to be current — from a publish, or from the direct
    /// /api/version read — so this instance answers with it immediately.</summary>
    public void Update(long newVersion)
    {
        lock (gate)
        {
            version = newVersion;
            lastAttemptAt = DateTimeOffset.UtcNow;
        }
    }
}
