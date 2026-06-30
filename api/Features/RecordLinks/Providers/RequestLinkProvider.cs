using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for the Request family (RFI/RFA/RFC/RFQ/RFP/NOD/EOT). Wraps the existing
// Requests table and the entity's computed TagReference; this is the provider behind the Request
// category in triage and behind the (now adapter) AssignMessageToRequest path.
public sealed class RequestLinkProvider : ILinkableRecordProvider
{
    private readonly JpmsContext context;

    public RequestLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.Request;

    // Every request reference family, plus the REQ-NNNN fallback used when a request has no human ref.
    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[]
    {
        "RFI", "RFA", "RFC", "RFQ", "RFP", "NOD", "EOT", "REQ"
    };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var entities = await context.Requests.AsNoTracking()
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.RaisedAt)
            .ToListAsync(ct);
        return entities.Select(ToLinkable).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var entity = await context.Requests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.RequestId == recordId, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    private static LinkableRecord ToLinkable(RequestEntity entity) => new(
        Type:         RecordType.Request,
        RecordId:     entity.RequestId,
        ProjectId:    entity.ProjectId,
        Reference:    string.IsNullOrWhiteSpace(entity.Reference) ? entity.TagReference : entity.Reference.Trim(),
        TagReference: entity.TagReference,
        Title:        entity.Title,
        StatusLabel:  ((RequestStatus)entity.Status).ToString());
}
