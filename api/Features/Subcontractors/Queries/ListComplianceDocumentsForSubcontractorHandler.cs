using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListComplianceDocumentsForSubcontractorHandler
    : IQueryHandler<ListComplianceDocumentsForSubcontractor, IReadOnlyList<ComplianceDocument>>
{
    private readonly JpmsContext context;

    public ListComplianceDocumentsForSubcontractorHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ComplianceDocument>> HandleAsync(ListComplianceDocumentsForSubcontractor query, CancellationToken cancellationToken)
    {
        var entities = await context.ComplianceDocuments.Where(document => document.SubcontractorId == query.SubcontractorId).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
