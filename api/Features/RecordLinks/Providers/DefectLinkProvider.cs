using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Procurement;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for defects. Wraps the Defects table so a triage email can be linked to
// a defect and the defect can read its mail back live by tag (RecordEmailReader) — the same
// mechanism the Bid Package and Work Order families use, with no changes to the link/read layer or
// triage UI.
//
// The pathway FOLLOWS THE COMPANY the defect is chased with (CompanyPathways, the road the work
// order took on 2026-09-15): the SAME DEF-#### record is raised from the Subcontractor pane (a
// trade's workmanship) and, since 2026-09-07, from the Supplier pane (a merchant's faulty or
// short-delivered goods), so the type alone cannot say which side a thread belongs to — the
// type is pathway-neutral since 2026-09-16. Every record this provider hands out carries
// LinkableRecord.Pathway: "Supplier" for a Supplier-category company, "Subcontractor" for any
// other and for a defect not yet assigned to a company; the pane that stages a tag can say
// otherwise (TriageCategories.BucketFor(LinkableRecord, chosen)).
public sealed class DefectLinkProvider : ILinkableRecordProvider, ITagResolvingProvider
{
    private readonly JpmsContext context;

    public DefectLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.Defect;

    // Defects own the "DEF" reference namespace.
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "DEF" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var rows = await WithCompanyAsync(
            context.Defects.AsNoTracking()
                .Where(d => d.ProjectId == projectId)
                .OrderByDescending(d => d.Number),
            ct);
        return rows.Select(row => ToLinkable(row.Defect, row.Category)).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var rows = await WithCompanyAsync(context.Defects.AsNoTracking().Where(d => d.DefectId == recordId), ct);
        return rows.Count == 0 ? null : ToLinkable(rows[0].Defect, rows[0].Category);
    }

    // "DEF-0004" -> the defect numbered 4 (global sequence, same flat-tag-space rule as to-dos).
    public async Task<LinkableRecord?> FindByTagAsync(string tagReference, CancellationToken ct)
    {
        if (!TagReferenceParsing.TryParseNumber(tagReference, "DEF", out var number)) return null;
        var rows = await WithCompanyAsync(context.Defects.AsNoTracking().Where(d => d.Number == number), ct);
        return rows.Count == 0 ? null : ToLinkable(rows[0].Defect, rows[0].Category);
    }

    // The company's directory category alongside each defect — which side its mail files under.
    // One projection, no per-row queries; null when the defect names no company.
    private async Task<List<(DefectEntity Defect, DirectoryCategory? Category)>> WithCompanyAsync(
        IQueryable<DefectEntity> defects, CancellationToken ct)
    {
        var rows = await defects
            .Select(defect => new
            {
                Defect = defect,
                Category = context.Subcontractors.AsNoTracking()
                    .Where(s => s.SubcontractorId == defect.SubcontractorId)
                    .Select(s => (int?)s.Category)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);
        return rows
            .Select(row => (row.Defect, row.Category is { } category ? (DirectoryCategory?)category : null))
            .ToList();
    }

    private static LinkableRecord ToLinkable(DefectEntity entity, DirectoryCategory? companyCategory)
    {
        // The defect's sequential DEF-0001 reference is the tag stem, so a triage email tagged to
        // it ("JPMS/DEF-0001") surfaces under the defect on the project's Defects tab.
        var reference = entity.Reference;

        // A defect has no title of its own: the location is what a triager reads first ("which
        // defect is the bathroom one?"), with the description as the fallback so a row is never
        // blank.
        var title = string.IsNullOrWhiteSpace(entity.Location)
            ? RecordSummaries.Clip(entity.Description) ?? reference
            : entity.Location;

        return new LinkableRecord(
            Type:         RecordType.Defect,
            RecordId:     entity.DefectId,
            ProjectId:    entity.ProjectId,
            Reference:    reference,
            TagReference: reference,
            Title:        title,
            StatusLabel:  ((DefectStatus)entity.Status).DisplayName(),
            Summary:      RecordSummaries.Clip(entity.Description),
            // Verified is the defect's closed-out state; everything before it is still being chased.
            IsActive:     entity.Status != (int)DefectStatus.Verified,
            // The side this defect's mail files under follows its company (see the class note).
            Pathway:      CompanyPathways.LabelFor(companyCategory));
    }
}
