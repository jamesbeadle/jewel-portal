using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Audit;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Audit.Queries;

// The audit register, newest first, offset-paged. Filters compose with AND; all optional.
public sealed class ListAuditEventsHandler : IQueryHandler<ListAuditEvents, AuditEventsPage>
{
    private readonly JpmsContext context;
    public ListAuditEventsHandler(JpmsContext context) { this.context = context; }

    public async Task<AuditEventsPage> HandleAsync(ListAuditEvents query, CancellationToken cancellationToken)
    {
        var events = context.AuditEvents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.ProjectId))
            events = events.Where(e => e.ProjectId == query.ProjectId);
        if (!string.IsNullOrWhiteSpace(query.Pathway))
            events = events.Where(e => e.Pathway == query.Pathway);
        if (query.EventType is { } eventType)
            events = events.Where(e => e.EventType == (int)eventType);
        if (!string.IsNullOrWhiteSpace(query.ActorEmail))
            events = events.Where(e => e.ActorEmail == query.ActorEmail);

        var total = await events.CountAsync(cancellationToken);

        var skip = 0;
        if (!string.IsNullOrEmpty(query.Cursor) && int.TryParse(query.Cursor, out var s) && s > 0)
            skip = s;
        var take = Math.Clamp(query.Take, 1, 200);

        var page = await events
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.AuditEventId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var items = page.Select(e => new AuditEvent(
            e.AuditEventId,
            e.OccurredAt,
            e.ActorEmail,
            (AuditEventType)e.EventType,
            e.Pathway,
            e.ProjectId,
            e.RecordType is { } rt ? (RecordType)rt : null,
            e.RecordId,
            e.RecordReference,
            e.ConversationId,
            e.EmailMessageId,
            e.InternetMessageId,
            e.WebLink,
            e.Detail)).ToList();

        var nextCursor = (skip + page.Count) < total ? (skip + take).ToString() : null;
        return new AuditEventsPage(items, nextCursor, total);
    }
}
