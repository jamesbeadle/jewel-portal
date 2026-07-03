using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for Liquidated Damages claims. Wraps the LadClaims table so a triage
// email (the client's LADs notice, correspondence disputing it, the settlement) can be linked to the
// claim and the claim reads its mail back live by tag — the same mechanism the Request and To-do
// families use. LAD claims also surface through the Scheduling picker (SchedulingLinkProvider lists
// them among the schedule's claims documents), but the tag/read layer resolves them here.
public sealed class LadLinkProvider : ILinkableRecordProvider
{
    private readonly JpmsContext context;

    public LadLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.Lad;

    // LADs claims own the "LAD" reference namespace.
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "LAD" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var entities = await context.LadClaims.AsNoTracking()
            .Where(l => l.ProjectId == projectId)
            .OrderByDescending(l => l.Number)
            .ToListAsync(ct);
        return entities.Select(ToLinkable).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var entity = await context.LadClaims.AsNoTracking()
            .FirstOrDefaultAsync(l => l.LadClaimId == recordId, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    private static LinkableRecord ToLinkable(LadClaimEntity entity)
    {
        // The claim's sequential LAD-0001 reference is the tag stem (globally unique, like TODO
        // numbers), so an email tagged "JPMS/LAD-0001" surfaces under the claim on the Schedule tab.
        var reference = entity.Reference;
        return new LinkableRecord(
            Type:         RecordType.Lad,
            RecordId:     entity.LadClaimId,
            ProjectId:    entity.ProjectId,
            Reference:    reference,
            TagReference: reference,
            Title:        entity.Title,
            StatusLabel:  ((LadStatus)entity.Status).DisplayName());
    }
}
