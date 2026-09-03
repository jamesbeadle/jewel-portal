using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for site instructions (2026-09-03). Wraps the SiteInstructions table so
// a triage email can be linked to an instruction and the instruction can read its mail back live
// by tag (RecordEmailReader) — the same mechanism defects and inventory use, with no changes to
// the link/read layer.
//
// Internal-side by construction: an instruction to site is Jewel telling its own people what to
// do, so TriageCategories.BucketFor maps the type to JPMS/Internal — the Internal pathway's first
// project-scoped linkable record with words of its own (replacing the record-less
// "IntComms-Site" category tag, which said nothing about WHAT the instruction was).
public sealed class SiteInstructionLinkProvider : ILinkableRecordProvider, ITagResolvingProvider
{
    private readonly JpmsContext context;

    public SiteInstructionLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.SiteInstruction;

    // Site instructions own the "SI" reference namespace.
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "SI" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var entities = await context.SiteInstructions.AsNoTracking()
            .Where(row => row.ProjectId == projectId)
            .OrderByDescending(row => row.Number)
            .ToListAsync(ct);
        return entities.Select(ToLinkable).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var entity = await context.SiteInstructions.AsNoTracking()
            .FirstOrDefaultAsync(row => row.SiteInstructionId == recordId, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    // "SI-0004" -> the instruction numbered 4 (global sequence, same flat-tag-space rule as defects).
    public async Task<LinkableRecord?> FindByTagAsync(string tagReference, CancellationToken ct)
    {
        if (!TagReferenceParsing.TryParseNumber(tagReference, "SI", out var number)) return null;
        var entity = await context.SiteInstructions.AsNoTracking()
            .FirstOrDefaultAsync(row => row.Number == number, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    private static LinkableRecord ToLinkable(SiteInstructionEntity entity)
    {
        // The instruction's sequential SI-0001 reference is the tag stem, so a triage email tagged
        // to it ("JPMS/SI-0001") surfaces under it on the project's Site Instructions page.
        var reference = entity.Reference;

        return new LinkableRecord(
            Type:         RecordType.SiteInstruction,
            RecordId:     entity.SiteInstructionId,
            ProjectId:    entity.ProjectId,
            Reference:    reference,
            TagReference: reference,
            Title:        string.IsNullOrWhiteSpace(entity.Title)
                              ? (RecordSummaries.Clip(entity.Instruction) ?? reference)
                              : entity.Title,
            StatusLabel:  string.IsNullOrWhiteSpace(entity.Location) ? null : entity.Location,
            Summary:      RecordSummaries.Clip(entity.Instruction),
            IsActive:     true);
    }
}
