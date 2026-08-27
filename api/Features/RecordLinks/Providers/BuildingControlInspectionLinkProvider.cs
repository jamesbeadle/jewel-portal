using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for building control INSPECTIONS. Wraps the inspections table so the
// inspector's booking/report thread can be linked to its stage and the stage reads its mail back
// live by tag (RecordEmailReader) — the Defect mechanism, with no changes to the link/read layer
// or triage UI.
public sealed class BuildingControlInspectionLinkProvider : ILinkableRecordProvider, ITagResolvingProvider
{
    private readonly JpmsContext context;

    public BuildingControlInspectionLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.BuildingControlInspection;

    // Inspections own the "BCI" reference namespace (cases own "BC").
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "BCI" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var entities = await context.BuildingControlInspections.AsNoTracking()
            .Where(row => row.ProjectId == projectId)
            .OrderBy(row => row.DisplayOrder)
            .ThenBy(row => row.Number)
            .ToListAsync(ct);
        return entities.Select(ToLinkable).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var entity = await context.BuildingControlInspections.AsNoTracking()
            .FirstOrDefaultAsync(row => row.BuildingControlInspectionId == recordId, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    // "BCI-0004" -> the inspection numbered 4 (global sequence, same flat-tag-space rule).
    public async Task<LinkableRecord?> FindByTagAsync(string tagReference, CancellationToken ct)
    {
        if (!TagReferenceParsing.TryParseNumber(tagReference, "BCI", out var number)) return null;
        var entity = await context.BuildingControlInspections.AsNoTracking()
            .FirstOrDefaultAsync(row => row.Number == number, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    private static LinkableRecord ToLinkable(BuildingControlInspectionEntity entity)
    {
        var reference = entity.Reference;
        return new LinkableRecord(
            Type:         RecordType.BuildingControlInspection,
            RecordId:     entity.BuildingControlInspectionId,
            ProjectId:    entity.ProjectId,
            Reference:    reference,
            TagReference: reference,
            Title:        string.IsNullOrWhiteSpace(entity.StageName) ? reference : entity.StageName,
            StatusLabel:  ((BuildingControlInspectionStatus)entity.Status).DisplayName(),
            Summary:      RecordSummaries.Clip(entity.OutcomeNotes),
            // Passed and Closed are the stage's finished states; everything before is live work.
            IsActive:     (BuildingControlInspectionStatus)entity.Status
                              is not (BuildingControlInspectionStatus.Passed or BuildingControlInspectionStatus.Closed));
    }
}
