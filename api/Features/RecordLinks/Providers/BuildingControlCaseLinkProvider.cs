using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.BuildingControl;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for building control CASES — case-level correspondence: the notice's
// acknowledgement, the "who is our contact" email, the completion certificate arriving. Each
// inspection's own thread files against the inspection (BuildingControlInspectionLinkProvider);
// this is for the mail that belongs to the case as a whole. Same live-read link mechanism as
// every other record — no changes to the link/read layer or triage UI.
public sealed class BuildingControlCaseLinkProvider : ILinkableRecordProvider, ITagResolvingProvider
{
    private readonly JpmsContext context;

    public BuildingControlCaseLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.BuildingControlCase;

    // Cases own the "BC" reference namespace (inspections own "BCI").
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "BC" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var entities = await context.BuildingControlCases.AsNoTracking()
            .Where(row => row.ProjectId == projectId)
            .OrderByDescending(row => row.Number)
            .ToListAsync(ct);
        return entities.Select(ToLinkable).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var entity = await context.BuildingControlCases.AsNoTracking()
            .FirstOrDefaultAsync(row => row.BuildingControlCaseId == recordId, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    // "BC-0001" -> the case numbered 1 (global sequence, same flat-tag-space rule as defects).
    public async Task<LinkableRecord?> FindByTagAsync(string tagReference, CancellationToken ct)
    {
        if (!TagReferenceParsing.TryParseNumber(tagReference, "BC", out var number)) return null;
        var entity = await context.BuildingControlCases.AsNoTracking()
            .FirstOrDefaultAsync(row => row.Number == number, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    private static LinkableRecord ToLinkable(BuildingControlCaseEntity entity)
    {
        var reference = entity.Reference;
        // The body's name is what a triager reads first ("the Assent case"); the body's own
        // reference is the summary, since their emails quote it.
        var title = string.IsNullOrWhiteSpace(entity.BodyName) ? reference : entity.BodyName;
        return new LinkableRecord(
            Type:         RecordType.BuildingControlCase,
            RecordId:     entity.BuildingControlCaseId,
            ProjectId:    entity.ProjectId,
            Reference:    reference,
            TagReference: reference,
            Title:        title,
            StatusLabel:  ((BuildingControlCaseStatus)entity.Status).DisplayName(),
            Summary:      RecordSummaries.Clip(entity.BodyReference),
            IsActive:     BuildingControlRules.IsActive(entity));
    }
}
