using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.TenderEnquiries;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// Linkable-record provider for tender enquiries. Wraps the TenderEnquiries table so the
// architect's invitation email (and everything that follows — the PQQ reply, the shortlist
// letter, the tender return) can be tagged "JPMS/TEQ-####" and read back live by the enquiry.
public sealed class TenderEnquiryLinkProvider : ILinkableRecordProvider, ITagResolvingProvider
{
    private const string Prefix = "TEQ";

    private readonly JpmsContext context;

    public TenderEnquiryLinkProvider(JpmsContext context) { this.context = context; }

    public RecordType Type => RecordType.TenderEnquiry;

    public IReadOnlyCollection<string> ReferencePrefixes { get; } = new[] { Prefix };

    public async Task<IReadOnlyList<LinkableRecord>> ForProjectAsync(string projectId, CancellationToken ct)
    {
        var entities = await context.TenderEnquiries.AsNoTracking()
            .Where(row => row.ProjectId == projectId)
            .OrderByDescending(row => row.ReceivedAt)
            .ToListAsync(ct);
        return entities.Select(ToLinkable).ToList().AsReadOnly();
    }

    public async Task<LinkableRecord?> FindAsync(string recordId, CancellationToken ct)
    {
        var entity = await context.TenderEnquiries.AsNoTracking()
            .FirstOrDefaultAsync(row => row.TenderEnquiryId == recordId, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    // "TEQ-0004" -> the enquiry numbered 4.
    public async Task<LinkableRecord?> FindByTagAsync(string tagReference, CancellationToken ct)
    {
        if (!TagReferenceParsing.TryParseNumber(tagReference, Prefix, out var number)) return null;
        var entity = await context.TenderEnquiries.AsNoTracking()
            .FirstOrDefaultAsync(row => row.Number == number, ct);
        return entity is null ? null : ToLinkable(entity);
    }

    private static LinkableRecord ToLinkable(TenderEnquiryEntity entity)
    {
        var model = entity.ToModel();
        return new LinkableRecord(
            Type:         RecordType.TenderEnquiry,
            RecordId:     entity.TenderEnquiryId,
            ProjectId:    entity.ProjectId,
            Reference:    model.Reference,
            TagReference: model.Reference,
            Title:        entity.Title,
            StatusLabel:  model.Status.DisplayName(),
            Summary:      RecordSummaries.Clip($"{entity.ArchitectPracticeName} — {entity.ScopeSummary}"),
            // Live while the bid is still in play; an ended enquiry (declined, not shortlisted,
            // won, lost) is finished business the picker reveals on request.
            IsActive:     model.Status.IsOpen());
    }
}
