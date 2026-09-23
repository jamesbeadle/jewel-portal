using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Queries;

/// <summary>The register, newest first, each record carrying its thread's standing — how many
/// comments, the latest, and whether a photograph of the work done is waiting on it.</summary>
public sealed class ListHsRecordsHandler : IQueryHandler<ListHsRecords, IReadOnlyList<HsRecord>>
{
    private readonly JpmsContext context;
    public ListHsRecordsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<HsRecord>> HandleAsync(ListHsRecords query, CancellationToken cancellationToken)
    {
        var entities = await context.HsRecords.AsNoTracking().OrderByDescending(record => record.RaisedAt).ToListAsync(cancellationToken);
        var standings = await ThreadStandingsAsync(cancellationToken);
        return entities.Select(entity => WithStanding(entity.ToModel(), standings)).ToList().AsReadOnly();
    }

    private sealed record ThreadStanding(int CommentCount, DateTimeOffset LastCommentAt, bool HasPhoto);

    private async Task<Dictionary<string, ThreadStanding>> ThreadStandingsAsync(CancellationToken cancellationToken)
    {
        var comments = await context.HsRecordComments.AsNoTracking()
            .GroupBy(comment => comment.HsRecordId)
            .Select(group => new { HsRecordId = group.Key, Count = group.Count(), Latest = group.Max(comment => comment.PostedAt) })
            .ToListAsync(cancellationToken);
        var photographed = await context.HsRecordPhotos.AsNoTracking()
            .Select(photo => photo.HsRecordId).Distinct().ToListAsync(cancellationToken);
        var withPhotos = photographed.ToHashSet(StringComparer.Ordinal);
        return comments.ToDictionary(
            row => row.HsRecordId,
            row => new ThreadStanding(row.Count, row.Latest, withPhotos.Contains(row.HsRecordId)),
            StringComparer.Ordinal);
    }

    private static HsRecord WithStanding(HsRecord record, IReadOnlyDictionary<string, ThreadStanding> standings)
    {
        var isDiscussed = standings.TryGetValue(record.HsRecordId, out var standing);
        if (!isDiscussed) return record;
        return record with { CommentCount = standing!.CommentCount, LastCommentAt = standing.LastCommentAt, HasPhoto = standing.HasPhoto };
    }
}
