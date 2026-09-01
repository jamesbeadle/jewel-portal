using Jewel.JPMS.Contracts.DocumentControl;

namespace Jewel.JPMS.Api.Features.DocumentControl.Queries;

// Every Document Control item, newest received first. All statuses in one read — the page splits
// its Queue / Filed / Discarded views client-side, so the three stay consistent with each other.
public sealed class ListDocumentControlItemsHandler
    : IQueryHandler<ListDocumentControlItems, IReadOnlyList<DocumentControlItem>>
{
    private readonly JpmsContext context;

    public ListDocumentControlItemsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<DocumentControlItem>> HandleAsync(
        ListDocumentControlItems query, CancellationToken cancellationToken)
    {
        var rows = await context.DocumentControlItems.AsNoTracking()
            .OrderByDescending(row => row.ReceivedAt)
            .ThenByDescending(row => row.SentAt)
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.ToModel()).ToList().AsReadOnly();
    }
}
