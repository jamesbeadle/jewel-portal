using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// Records the client's payment against a valuation invoice and rolls the amount received into the
/// project-level ValuationInvoicePaidTotal in the same transaction.
/// </summary>
public sealed class RecordValuationInvoicePaymentHandler : ICommandHandler<RecordValuationInvoicePayment, ValuationInvoice>
{
    private readonly JpmsContext context;
    public RecordValuationInvoicePaymentHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationInvoice> HandleAsync(RecordValuationInvoicePayment command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationInvoices.FindAsync(new object[] { command.ValuationInvoiceId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Valuation invoice {command.ValuationInvoiceId} not found.");
        if (entity.Status == (int)ValuationInvoiceStatus.Paid)
            throw new InvalidOperationException("This valuation invoice has already been paid.");

        var project = await context.Projects.FindAsync(new object[] { entity.ProjectId }, cancellationToken);
        if (project is null) throw new InvalidOperationException($"Project {entity.ProjectId} not found.");

        entity.AmountPaid = command.AmountPaid;
        entity.Status = (int)ValuationInvoiceStatus.Paid;
        entity.PaidAt = DateTimeOffset.UtcNow;

        project.ValuationInvoicePaidTotal += command.AmountPaid;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
