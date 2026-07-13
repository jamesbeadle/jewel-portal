using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Subcontractors;
using Jewel.JPMS.Contracts.Portal;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Portal.Queries;

public sealed class GetMyPortalRecordHandler : IQueryHandler<GetMyPortalRecord, SubcontractorPortalRecord?>
{
    private readonly JpmsContext context;

    public GetMyPortalRecordHandler(JpmsContext context) { this.context = context; }

    public async Task<SubcontractorPortalRecord?> HandleAsync(GetMyPortalRecord query, CancellationToken cancellationToken)
    {
        var entity = await context.Subcontractors
            .FirstOrDefaultAsync(row => row.SubcontractorId == query.SubcontractorId, cancellationToken);
        if (entity is null) return null;

        var tradesBySubcontractor = await context.TradesBySubcontractorAsync(cancellationToken);
        var trades = tradesBySubcontractor.TryGetValue(entity.SubcontractorId, out var found)
            ? found : Array.Empty<Trade>();

        var documents = await context.ComplianceDocuments
            .Where(row => row.SubcontractorId == query.SubcontractorId)
            .OrderByDescending(row => row.UploadedAt)
            .ToListAsync(cancellationToken);

        return new SubcontractorPortalRecord(
            entity.ToModel(trades),
            documents.Select(document => document.ToModel()).ToList().AsReadOnly());
    }
}
