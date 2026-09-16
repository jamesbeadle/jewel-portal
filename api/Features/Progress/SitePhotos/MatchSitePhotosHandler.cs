using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>Answers one match per hash asked, in the order asked. Hashes are compared as the pool
/// stores them — lower-case hex — so a hash pasted in capitals still finds its photo.</summary>
public sealed class MatchSitePhotosHandler : IQueryHandler<MatchSitePhotos, SitePhotoMatches>
{
    private readonly JpmsContext context;

    public MatchSitePhotosHandler(JpmsContext context) { this.context = context; }

    public async Task<SitePhotoMatches> HandleAsync(MatchSitePhotos query, CancellationToken cancellationToken)
    {
        var asked = query.ContentHashes.Select(Normalise).Where(hash => hash.Length > 0).Distinct().ToList();
        var found = await context.SitePhotos.AsNoTracking()
            .Where(row => asked.Contains(row.ContentHash))
            .ToDictionaryAsync(row => row.ContentHash, row => row.ToModel(), StringComparer.Ordinal, cancellationToken);

        var matches = query.ContentHashes
            .Select(hash => new SitePhotoMatch(hash, found.GetValueOrDefault(Normalise(hash))))
            .ToList();
        return new SitePhotoMatches(matches);
    }

    private static string Normalise(string hash) => hash.Trim().ToLowerInvariant();
}
