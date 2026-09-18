using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// Raised -> Submitted: records that the statement went to the client. The statement is the
/// locked claim the invoice is drawn against (2026-09-18); nothing is frozen or re-frozen here.
/// </summary>
public sealed class SubmitValuationInvoiceHandler : ICommandHandler<SubmitValuationInvoice, ValuationInvoice>
{
    private readonly JpmsContext context;
    public SubmitValuationInvoiceHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationInvoice> HandleAsync(SubmitValuationInvoice command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationInvoices.FindAsync(new object[] { command.ValuationInvoiceId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Valuation invoice {command.ValuationInvoiceId} not found.");
        if (entity.IsManual)
            throw new InvalidOperationException("Manual (historic) invoices record history — they don't go through approval.");
        if (entity.Status != (int)ValuationInvoiceStatus.Raised)
            throw new InvalidOperationException("Only a Raised valuation invoice can be submitted for approval.");

        entity.Status = (int)ValuationInvoiceStatus.Submitted;
        entity.SubmittedAt = DateTimeOffset.UtcNow;

        ValuationInvoiceAuditTrail.Append(context, entity.ValuationInvoiceId,
            ValuationInvoiceEventType.Submitted, "Submitted for approval.", amountAfter: entity.Amount);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
