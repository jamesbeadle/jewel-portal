using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Queries;

public sealed class ListValuationInvoiceEventsHandler : IQueryHandler<ListValuationInvoiceEvents, IReadOnlyList<ValuationInvoiceEvent>>
{
    private readonly JpmsContext context;
    public ListValuationInvoiceEventsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ValuationInvoiceEvent>> HandleAsync(ListValuationInvoiceEvents query, CancellationToken cancellationToken)
    {
        var events = await context.ValuationInvoiceEvents
            .Where(entry => entry.ValuationInvoiceId == query.ValuationInvoiceId)
            .OrderBy(entry => entry.OccurredAt)
            .ToListAsync(cancellationToken);
        return events.Select(entry => entry.ToModel()).ToList();
    }
}
