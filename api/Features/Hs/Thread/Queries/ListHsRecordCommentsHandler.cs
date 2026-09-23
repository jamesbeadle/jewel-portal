using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Thread.Queries;

/// <summary>A record's thread, oldest first, each comment carrying its photographs.</summary>
public sealed class ListHsRecordCommentsHandler : IQueryHandler<ListHsRecordComments, IReadOnlyList<HsRecordComment>>
{
    private readonly JpmsContext context;
    public ListHsRecordCommentsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<HsRecordComment>> HandleAsync(ListHsRecordComments query, CancellationToken cancellationToken)
    {
        var comments = await context.HsRecordComments.AsNoTracking()
            .Where(row => row.HsRecordId == query.HsRecordId)
            .OrderBy(row => row.PostedAt)
            .ToListAsync(cancellationToken);
        var photos = await context.HsRecordPhotos.AsNoTracking()
            .Where(row => row.HsRecordId == query.HsRecordId)
            .ToListAsync(cancellationToken);
        var photosByComment = photos.ToLookup(photo => photo.HsRecordCommentId);
        return comments.Select(comment => comment.ToModel(photosByComment[comment.HsRecordCommentId])).ToList();
    }
}
