using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Queries;

public sealed class ListValuationInvoiceEventsHandler : IQueryHandler<ListValuationInvoiceEvents, IReadOnlyList<ValuationInvoiceEvent>>
{
    private readonly JpmsContext context;
    public ListValuationInvoiceEventsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ValuationInvoiceEvent>> HandleAsync(ListValuationInvoiceEvents query, CancellationToken cancellationToken)
    {
        var events = await context.ValuationInvoiceEvents.AsNoTracking()
            .Where(entry => entry.ValuationInvoiceId == query.ValuationInvoiceId)
            .OrderBy(entry => entry.OccurredAt)
            .ToListAsync(cancellationToken);
        return events.Select(entry => entry.ToModel()).ToList();
    }
}
