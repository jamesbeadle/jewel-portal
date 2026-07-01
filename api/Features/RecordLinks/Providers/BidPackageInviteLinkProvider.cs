using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for Bid Package Invites. Wraps the BidPackages table so a tagged email can
// be linked to a bid package and the package can read its mail back live by tag (RecordEmailReader) —
// the same mechanism the Request family uses, with no changes to the link/read layer or triage UI.
public sealed class BidPackageInviteLinkProvider : ILinkableRecordProvider
{
    private readonly JpmsContext context;

    public BidPackageInviteLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.BidPackageInvite;

    // Bid packages own the "BPI" reference namespace.
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { "BPI" };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var entities = await context.BidPackages.AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
        return entities.Select(ToLinkable).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var entity = await context.BidPackages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.BidPackageId == recordId, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    private static LinkableRecord ToLinkable(BidPackageEntity entity)
    {
        // The package's sequential BPI-0001 reference is the tag stem, so a triage email tagged to it
        // ("JPMS/BPI-0001") surfaces under the package in the Bid Package Invites section. Legacy rows
        // with no Number fall back to the id-derived stem (BidPackageEntity.Reference handles both).
        var reference = entity.Reference;
        return new LinkableRecord(
            Type:         RecordType.BidPackageInvite,
            RecordId:     entity.BidPackageId,
            ProjectId:    entity.ProjectId,
            Reference:    reference,
            TagReference: reference,
            Title:        entity.Title,
            StatusLabel:  ((BidPackageStatus)entity.Status).ToString());
    }
}
