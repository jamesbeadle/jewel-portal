using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

// Per-record activity from the audit trail's link events: one grouped read over the project's
// last WindowDays of EmailTriaged / RecordLinked / RecordCreatedFromEmail rows (ThreadSwept counts
// the day it is ever written), scored through the one shared ActivityScore. The existing
// IX_AuditEvents_ProjectId index carries the filter — no schema change was needed for this query.
public sealed class ListRecordActivityHandler
    : IQueryHandler<ListRecordActivity, IReadOnlyList<RecordActivitySummary>>
{
    // The events that mean "correspondence landed on this record". Deliberately excludes
    // DraftCreated (outbound — the PM did that themselves), TagRemoved/Discarded/WallRejected
    // (nothing landed) and the finance lifecycle events.
    private static readonly int[] ActivityEventTypes =
    {
        (int)AuditEventType.EmailTriaged,
        (int)AuditEventType.RecordLinked,
        (int)AuditEventType.RecordCreatedFromEmail,
        (int)AuditEventType.ThreadSwept
    };

    private readonly JpmsContext context;

    public ListRecordActivityHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<RecordActivitySummary>> HandleAsync(
        ListRecordActivity query, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var windowStart = now.AddDays(-ActivityScore.WindowDays);

        var events = await context.AuditEvents.AsNoTracking()
            .Where(e => e.ProjectId == query.ProjectId
                && e.OccurredAt >= windowStart
                && e.RecordType != null
                && e.RecordId != null
                && ActivityEventTypes.Contains(e.EventType))
            .Select(e => new { e.RecordType, e.RecordId, e.RecordReference, e.OccurredAt })
            .ToListAsync(cancellationToken);

        var last7Start = now.AddDays(-7);

        return events
            .GroupBy(e => (Type: Coalesce((RecordType)e.RecordType!.Value), RecordId: e.RecordId!))
            .Select(group =>
            {
                var newestFirst = group.OrderByDescending(e => e.OccurredAt).ToList();
                return new RecordActivitySummary(
                    group.Key.Type,
                    group.Key.RecordId,
                    newestFirst[0].RecordReference,
                    newestFirst.Count(e => e.OccurredAt >= last7Start),
                    newestFirst[0].OccurredAt,
                    ActivityScore.For(newestFirst.Select(e => e.OccurredAt), now));
            })
            .ToList();
    }

    // A variation is ONE document (see CLAUDE.md terminology): both its link providers already use
    // the same VariationOrderId as RecordId, but rows written pre-approval carry the persisted
    // RecordType.VariationQuote while post-approval links carry Variation. Coalesce them so V72's
    // badge counts its whole correspondence, not the post-approval slice.
    private static RecordType Coalesce(RecordType type) =>
        type == RecordType.VariationQuote ? RecordType.Variation : type;
}
