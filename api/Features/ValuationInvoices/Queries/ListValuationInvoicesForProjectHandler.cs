using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Queries;

public sealed class ListValuationInvoicesForProjectHandler : IQueryHandler<ListValuationInvoicesForProject, IReadOnlyList<ValuationInvoice>>
{
    private readonly JpmsContext context;
    public ListValuationInvoicesForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ValuationInvoice>> HandleAsync(ListValuationInvoicesForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.ValuationInvoices
            .Where(call => call.ProjectId == query.ProjectId)
            .OrderByDescending(call => call.Number)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
