using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

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
public sealed class VariationOrderLinkProvider : ILinkableRecordProvider
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
            .Where(v => v.ProjectId == projectId && v.VariationRef != null)
            .OrderByDescending(v => v.Number)
            .ToListAsync(ct);
        return entities.Select(v => ToLinkable(v, projectRef)).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var entity = await context.VariationOrders.AsNoTracking()
            .FirstOrDefaultAsync(v => v.VariationOrderId == recordId, ct);
        if (entity is null || entity.VariationRef is null) return null;

        var projectRef = await ProjectRefAsync(entity.ProjectId, ct);
        return ToLinkable(entity, projectRef);
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
            StatusLabel:  ((VariationOrderStatus)entity.Status).ToString(),
            Summary:      RecordSummaries.Clip(entity.Description));
    }
}
