using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for Variation Order Quotes — the pre-approval stage of a variation, where
// most of the correspondence actually happens (the client's request, subcontractor pricing, the
// architect's comments). Wraps the VariationOrderQuotes table so a triage email can be linked to the
// VOQ it concerns; once the VOQ is approved into a VO, later instruction-stage mail can be linked to
// the VO record instead — the two are separate records with separate tag stems.
//
// VOQ references ("VOQ-0004") are only unique per project, while JPMS tags share one flat
// mailbox-category space — so the tag stem is project-qualified the same way VO tags are, using the
// bare number to avoid stuttering the VOQ prefix:
//   TagReference = "VOQ-{projectRef}-{number}"  ->  category "JPMS/VOQ-JBB-2026-002-0004".
public sealed class VariationOrderQuoteLinkProvider : ILinkableRecordProvider
{
    private readonly JpmsContext context;

    public VariationOrderQuoteLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.VariationQuote;

    // VOQs own the "VOQ" reference namespace (tags are "VOQ-<projectRef>-<number>"). Distinct from
    // the Variation Orders' "VO" prefix, so the flat tag space stays collision-free.
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "VOQ" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var projectRef = await ProjectRefAsync(projectId, ct);
        var entities = await context.VariationOrderQuotes.AsNoTracking()
            .Where(v => v.ProjectId == projectId)
            .OrderByDescending(v => v.Number)
            .ToListAsync(ct);
        return entities.Select(v => ToLinkable(v, projectRef)).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var entity = await context.VariationOrderQuotes.AsNoTracking()
            .FirstOrDefaultAsync(v => v.VariationOrderQuoteId == recordId, ct);
        if (entity is null) return null;

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
        // stem stays project-unique either way — same rule as the VO and cost-centre providers.
        return string.IsNullOrWhiteSpace(reference) ? projectId : reference.Trim();
    }

    private static LinkableRecord ToLinkable(VariationOrderQuoteEntity entity, string projectRef)
    {
        // Show the per-project "VOQ-0004" reference in the picker; qualify the tag stem with the
        // project and the bare number so the stem doesn't stutter ("VOQ-…-VOQ-0004").
        var reference = string.IsNullOrWhiteSpace(entity.Reference) ? $"VOQ-{entity.Number:0000}" : entity.Reference.Trim();
        return new LinkableRecord(
            Type:         RecordType.VariationQuote,
            RecordId:     entity.VariationOrderQuoteId,
            ProjectId:    entity.ProjectId,
            Reference:    reference,
            TagReference: $"VOQ-{projectRef}-{entity.Number:0000}",
            Title:        entity.Title,
            StatusLabel:  ((VariationOrderQuoteStatus)entity.Status).ToString(),
            Summary:      RecordSummaries.Clip(entity.Description));
    }
}
