using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for inventory items. Wraps the InventoryItems table so a triage email
// can be linked to an item and the item can read its mail back live by tag (RecordEmailReader) —
// the same mechanism defects use, with no changes to the link/read layer or triage UI.
//
// Supplier-side by construction: the goods come from a materials/goods supplier, so
// TriageCategories.BucketFor maps the type to JPMS/Supplier — the Supplier pathway's first
// linkable record type (2026-08-28).
public sealed class InventoryLinkProvider : ILinkableRecordProvider, ITagResolvingProvider
{
    private readonly JpmsContext context;

    public InventoryLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.Inventory;

    // Inventory items own the "INV" reference namespace.
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "INV" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var entities = await context.InventoryItems.AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .OrderByDescending(item => item.Number)
            .ToListAsync(ct);
        return entities.Select(ToLinkable).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var entity = await context.InventoryItems.AsNoTracking()
            .FirstOrDefaultAsync(item => item.InventoryItemId == recordId, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    // "INV-0004" -> the item numbered 4 (global sequence, same flat-tag-space rule as defects).
    public async Task<LinkableRecord?> FindByTagAsync(string tagReference, CancellationToken ct)
    {
        if (!TagReferenceParsing.TryParseNumber(tagReference, "INV", out var number)) return null;
        var entity = await context.InventoryItems.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Number == number, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    private static LinkableRecord ToLinkable(InventoryItemEntity entity)
    {
        // The item's sequential INV-0001 reference is the tag stem, so a triage email tagged to
        // it ("JPMS/INV-0001") surfaces under the item on the project's Inventory tab.
        var reference = entity.Reference;

        return new LinkableRecord(
            Type:         RecordType.Inventory,
            RecordId:     entity.InventoryItemId,
            ProjectId:    entity.ProjectId,
            Reference:    reference,
            TagReference: reference,
            Title:        string.IsNullOrWhiteSpace(entity.ProductName)
                              ? (RecordSummaries.Clip(entity.ProductDetails) ?? reference)
                              : entity.ProductName,
            StatusLabel:  string.IsNullOrWhiteSpace(entity.Location) ? null : entity.Location,
            Summary:      RecordSummaries.Clip(entity.ProductDetails),
            IsActive:     true);
    }
}
