using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider surfacing a variation order by its contract-stage "V18" reference — the
// instruction-stage identity, used once the variation is approved. It wraps the same unified
// VariationOrderQuotes table as the VOQ provider (a variation is ONE document since the 2026-07-23
// unification); the two providers just offer two references onto it, so both the historic VO- and
// VOQ- mail tags keep resolving to the record. Only approved orders carry a V-ref, so this provider
// lists just those; a still-quoting order is reachable by its VOQ- reference instead.
//
// Variation references ("V18") are only unique per project, while JPMS tags share one flat
// mailbox-category space — so the tag stem is project-qualified the same way cost-centre tags are:
//   TagReference = "VO-{projectRef}-{variationRef}"  ->  category "JPMS/VO-JBB-2026-001-V18".
//
// ForProjectAsync goes wider than the VO identity: the picker lists EVERY stage of the project's
// variations (one document, one number — see CLAUDE.md), so an email about a still-quoting
// variation can be linked to it. Pre-approval rows are handed out under their VariationQuote
// identity — the stable VOQ- tag stem — because the V-ref is only minted at approval and a guessed
// stem would silently detach mail the moment the real one lands. The variation page already reads
// both tags' mail, so either identity surfaces on the same record.
public sealed class VariationOrderLinkProvider : ILinkableRecordProvider, ITagResolvingProvider
{
    private readonly JpmsContext context;

    public VariationOrderLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.Variation;

    // Variation Orders own the "VO" reference namespace (tags are "VO-<projectRef>-<variationRef>").
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "VO" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var projectRef = await ProjectRefAsync(projectId, ct);
        var entities = await context.VariationOrders.AsNoTracking()
            .Where(v => v.ProjectId == projectId)
            .OrderByDescending(v => v.Number)
            .ToListAsync(ct);
        return entities
            .Select(v => v.VariationRef is null ? ToLinkableQuoteStage(v, projectRef) : ToLinkable(v, projectRef))
            .ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var entity = await context.VariationOrders.AsNoTracking()
            .FirstOrDefaultAsync(v => v.VariationOrderId == recordId, ct);
        if (entity is null || entity.VariationRef is null) return null;

        var projectRef = await ProjectRefAsync(entity.ProjectId, ct);
        return ToLinkable(entity, projectRef);
    }

    // Reverse lookup for the tagged-email search: "VO-{projectRef}-{variationRef}". The variation
    // ref is the last segment ("V18"); candidates carrying it are verified against their own full
    // project-qualified stem, which is what disambiguates two projects' V18.
    public async Task<LinkableRecord?> FindByTagAsync(string tagReference, CancellationToken ct)
    {
        if (!tagReference.StartsWith("VO-", StringComparison.OrdinalIgnoreCase)) return null;
        var lastDash = tagReference.LastIndexOf('-');
        if (lastDash < 3 || lastDash == tagReference.Length - 1) return null;
        var variationRef = tagReference[(lastDash + 1)..];

        var candidates = await context.VariationOrders.AsNoTracking()
            .Where(v => v.VariationRef == variationRef)
            .Take(10)
            .ToListAsync(ct);
        foreach (var entity in candidates)
        {
            var record = ToLinkable(entity, await ProjectRefAsync(entity.ProjectId, ct));
            if (record.TagReference.Equals(tagReference, StringComparison.OrdinalIgnoreCase))
                return record;
        }
        return null;
    }

    private async Task<string> ProjectRefAsync(string projectId, CancellationToken ct)
    {
        var reference = await context.Projects.AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .Select(p => p.Reference)
            .FirstOrDefaultAsync(ct);
        // Fall back to the (unique) project id if the project has no human reference yet, so the tag
        // stem stays project-unique either way — same rule as the cost-centre provider.
        return string.IsNullOrWhiteSpace(reference) ? projectId : reference.Trim();
    }

    private static LinkableRecord ToLinkable(VariationOrderEntity entity, string projectRef)
    {
        // Show the per-project "V18" reference in the picker; qualify the tag stem with the project.
        var variationRef = string.IsNullOrWhiteSpace(entity.VariationRef) ? $"V{entity.Number:00}" : entity.VariationRef.Trim();
        return new LinkableRecord(
            Type:         RecordType.Variation,
            RecordId:     entity.VariationOrderId,
            ProjectId:    entity.ProjectId,
            Reference:    variationRef,
            TagReference: $"VO-{projectRef}-{variationRef}",
            Title:        entity.Title,
            StatusLabel:  ((VariationOrderStatus)entity.Status).DisplayName(),
            Summary:      RecordSummaries.Clip(entity.Description),
            IsActive:     entity.Status != (int)VariationOrderStatus.Rejected);
    }

    // A variation with no minted V-ref yet, listed under its stable quoting-stage identity: the
    // user still reads the one number ("V72"), the tag is the VOQ- stem historic mail already uses,
    // and the link/read layer resolves it through the VariationQuote provider.
    private static LinkableRecord ToLinkableQuoteStage(VariationOrderEntity entity, string projectRef)
    {
        var displayNumber = entity.Number > 0 ? $"V{entity.Number}" : entity.Reference.Trim();
        return new LinkableRecord(
            Type:         RecordType.VariationQuote,
            RecordId:     entity.VariationOrderId,
            ProjectId:    entity.ProjectId,
            Reference:    displayNumber,
            TagReference: $"VOQ-{projectRef}-{entity.Number:0000}",
            Title:        entity.Title,
            StatusLabel:  ((VariationOrderStatus)entity.Status).DisplayName(),
            Summary:      RecordSummaries.Clip(entity.Description),
            IsActive:     entity.Status != (int)VariationOrderStatus.Rejected);
    }
}
