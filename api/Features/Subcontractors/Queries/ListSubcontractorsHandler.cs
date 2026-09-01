using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListSubcontractorsHandler
    : IQueryHandler<ListSubcontractors, IReadOnlyList<Subcontractor>>
{
    private readonly JpmsContext context;

    public ListSubcontractorsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Subcontractor>> HandleAsync(ListSubcontractors query, CancellationToken cancellationToken)
    {
        var entities = await context.Subcontractors.AsNoTracking().OrderBy(sub => sub.CompanyName).ToListAsync(cancellationToken);
        var tradesBySubcontractor = await context.TradesBySubcontractorAsync(cancellationToken);

        // The Xero link mark: a record holding at least one Xero link (imported from Xero, or a
        // Xero-imported record was consolidated into it) shows as linked.
        var xeroLinkedIds = (await context.SubcontractorXeroLinks.AsNoTracking()
                .Select(link => link.SubcontractorId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return entities
            .Select(entity => entity.ToModel(
                tradesBySubcontractor.TryGetValue(entity.SubcontractorId, out var trades) ? trades : Array.Empty<Trade>(),
                xeroLinked: xeroLinkedIds.Contains(entity.SubcontractorId)))
            .ToList()
            .AsReadOnly();
    }
}
