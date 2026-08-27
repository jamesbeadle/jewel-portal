using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for project calendar events. Wraps the CalendarEvents table so a
// triage email can be linked to an event and the event can read its mail back live by tag
// (RecordEmailReader) — the same mechanism the To-do family uses, with no changes to the
// link/read layer or triage UI.
public sealed class CalendarEventLinkProvider : ILinkableRecordProvider, ITagResolvingProvider
{
    private readonly JpmsContext context;

    public CalendarEventLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.CalendarEvent;

    // Calendar events own the "CAL" reference namespace.
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "CAL" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var entities = await context.CalendarEvents.AsNoTracking()
            .Where(e => e.ProjectId == projectId)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Number)
            .ToListAsync(ct);
        return entities.Select(ToLinkable).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var entity = await context.CalendarEvents.AsNoTracking()
            .FirstOrDefaultAsync(e => e.CalendarEventId == recordId, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    // "CAL-0011" -> the event numbered 11. Numbers are global (the tag space is flat), and the
    // Reference itself is computed-not-stored, so the number is the queryable key.
    public async Task<LinkableRecord?> FindByTagAsync(string tagReference, CancellationToken ct)
    {
        if (!TagReferenceParsing.TryParseNumber(tagReference, "CAL", out var number)) return null;
        var entity = await context.CalendarEvents.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Number == number, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    private static LinkableRecord ToLinkable(CalendarEventEntity entity)
    {
        var reference = entity.Reference;
        var today = SiteClock.Today();
        var lastDay = entity.EndDate ?? entity.Date;
        return new LinkableRecord(
            Type:         RecordType.CalendarEvent,
            RecordId:     entity.CalendarEventId,
            ProjectId:    entity.ProjectId,
            Reference:    reference,
            TagReference: reference,
            Title:        entity.Title,
            StatusLabel:  lastDay < today ? "Past" : entity.Date <= today ? "Today" : "Upcoming",
            Summary:      RecordSummaries.Clip(entity.Notes),
            IsActive:     lastDay >= today);
    }
}
