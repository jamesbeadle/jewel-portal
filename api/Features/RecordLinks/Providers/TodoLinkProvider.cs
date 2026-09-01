using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for project to-do items. Wraps the TodoItems table so a triage email can
// be linked to a to-do item and the item can read its mail back live by tag (RecordEmailReader) —
// the same mechanism the Request and Bid Package families use, with no changes to the link/read
// layer or triage UI.
public sealed class TodoLinkProvider : ILinkableRecordProvider, ITagResolvingProvider
{
    private readonly JpmsContext context;

    public TodoLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.Todo;

    // To-do items own the "TODO" reference namespace.
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "TODO" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var entities = await context.TodoItems.AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .OrderByDescending(t => t.Number)
            .ToListAsync(ct);
        return entities.Select(ToLinkable).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var entity = await context.TodoItems.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TodoItemId == recordId, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    // "TODO-0011" -> the item numbered 11. Numbers are global (the tag space is flat), and the
    // Reference itself is computed-not-stored, so the number is the queryable key.
    public async Task<LinkableRecord?> FindByTagAsync(string tagReference, CancellationToken ct)
    {
        if (!TagReferenceParsing.TryParseNumber(tagReference, "TODO", out var number)) return null;
        var entity = await context.TodoItems.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Number == number, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    private static LinkableRecord ToLinkable(TodoItemEntity entity)
    {
        // The item's sequential TODO-0001 reference is the tag stem, so a triage email tagged to it
        // ("JPMS/TODO-0001") surfaces under the item on the project's Overview tab.
        var reference = entity.Reference;
        return new LinkableRecord(
            Type:         RecordType.Todo,
            RecordId:     entity.TodoItemId,
            ProjectId:    entity.ProjectId,
            Reference:    reference,
            TagReference: reference,
            Title:        entity.Title,
            StatusLabel:  entity.IsComplete ? "Done" : entity.StartedAt is null ? "Open" : "In progress",
            Summary:      RecordSummaries.Clip(entity.Notes),
            IsActive:     !entity.IsComplete);
    }
}
