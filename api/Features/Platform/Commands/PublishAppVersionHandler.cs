using Jewel.JPMS.Contracts.Platform;

namespace Jewel.JPMS.Api.Features.Platform.Commands;

/// <summary>
/// Bumps the announced version by one and stamps who did it. Every open tab then sees a number
/// higher than the one it baselined on — on its next data fetch via the response header, or on
/// regaining focus via /api/version — and the UpdateToast offers the refresh. An increment
/// rather than a client-supplied number: nothing to mistype, and the number can never move
/// backwards (a lower announcement would prompt nobody and only sow confusion).
/// </summary>
public sealed class PublishAppVersionHandler
    : ICommandHandler<PublishAppVersion, AnnouncedAppVersion>
{
    private readonly JpmsContext context;
    private readonly AnnouncedVersionCache cache;

    public PublishAppVersionHandler(JpmsContext context, AnnouncedVersionCache cache)
    {
        this.context = context;
        this.cache = cache;
    }

    public async Task<AnnouncedAppVersion> HandleAsync(
        PublishAppVersion command, CancellationToken cancellationToken)
    {
        var row = await context.AppVersions
            .FirstOrDefaultAsync(
                current => current.AppVersionId == AnnouncedVersionCache.CurrentRowId,
                cancellationToken);
        if (row is null)
            // The AddAppVersions migration seeds the row. Recreating it here would restart the
            // count at 1 — below every open tab's baseline, so publishes would silently prompt
            // nobody. Fail loudly instead, like the query does.
            throw new InvalidOperationException(
                "The announced-version row is missing — has the AddAppVersions migration been applied?");

        row.Version += 1;
        row.PublishedAt = DateTimeOffset.UtcNow;
        row.PublishedBy = command.PublishedBy;
        await context.SaveChangesAsync(cancellationToken);

        // This instance answers with the new number immediately — including on this very
        // response's header; the other instances catch up within the cache TTL.
        cache.Update(row.Version);
        return new AnnouncedAppVersion(row.Version, row.PublishedAt, row.PublishedBy);
    }
}
