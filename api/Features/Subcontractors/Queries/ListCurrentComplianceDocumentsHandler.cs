using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListCurrentComplianceDocumentsHandler
    : IQueryHandler<ListCurrentComplianceDocuments, IReadOnlyList<ComplianceDocument>>
{
    private readonly JpmsContext context;

    public ListCurrentComplianceDocumentsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ComplianceDocument>> HandleAsync(ListCurrentComplianceDocuments query, CancellationToken cancellationToken)
    {
        var entities = await context.ComplianceDocuments.AsNoTracking()
            .Where(document => document.SupersededAt == null)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
