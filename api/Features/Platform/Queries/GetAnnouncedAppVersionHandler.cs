using Jewel.JPMS.Contracts.Platform;

namespace Jewel.JPMS.Api.Features.Platform.Queries;

public sealed class GetAnnouncedAppVersionHandler
    : IQueryHandler<GetAnnouncedAppVersion, AnnouncedAppVersion>
{
    private readonly JpmsContext context;

    public GetAnnouncedAppVersionHandler(JpmsContext context) { this.context = context; }

    public async Task<AnnouncedAppVersion> HandleAsync(
        GetAnnouncedAppVersion query, CancellationToken cancellationToken)
    {
        var row = await context.AppVersions.AsNoTracking()
            .FirstOrDefaultAsync(
                current => current.AppVersionId == AnnouncedVersionCache.CurrentRowId,
                cancellationToken);
        if (row is null)
            // The AddAppVersions migration seeds the row, so this is the schema running behind
            // the code — fail loudly rather than show a made-up v0 (see api/Program.cs on why
            // migrations never run themselves).
            throw new InvalidOperationException(
                "The announced-version row is missing — has the AddAppVersions migration been applied?");
        return new AnnouncedAppVersion(row.Version, row.PublishedAt, row.PublishedBy);
    }
}
