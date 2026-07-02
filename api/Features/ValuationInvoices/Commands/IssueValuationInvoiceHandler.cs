using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

public sealed class IssueValuationInvoiceHandler : ICommandHandler<IssueValuationInvoice, ValuationInvoice>
{
    private readonly JpmsContext context;
    public IssueValuationInvoiceHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationInvoice> HandleAsync(IssueValuationInvoice command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationInvoices.FindAsync(new object[] { command.ValuationInvoiceId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Valuation invoice {command.ValuationInvoiceId} not found.");
        if (entity.Status == (int)ValuationInvoiceStatus.Paid)
            throw new InvalidOperationException("A paid valuation invoice cannot be re-issued.");

        entity.Status = (int)ValuationInvoiceStatus.Issued;
        entity.IssuedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
