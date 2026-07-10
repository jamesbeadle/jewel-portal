using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// Submitted -> Rejected. The reason is required — it drives the amendment. The invoice is
/// unlocked: amend it (returns to Raised, fresh snapshot on resubmit) or cancel it.
/// </summary>
public sealed class RejectValuationInvoiceHandler : ICommandHandler<RejectValuationInvoice, ValuationInvoice>
{
    private readonly JpmsContext context;
    public RejectValuationInvoiceHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationInvoice> HandleAsync(RejectValuationInvoice command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationInvoices.FindAsync(new object[] { command.ValuationInvoiceId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Valuation invoice {command.ValuationInvoiceId} not found.");
        if (entity.Status != (int)ValuationInvoiceStatus.Submitted)
            throw new InvalidOperationException("Only a Submitted valuation invoice can be rejected.");

        entity.Status = (int)ValuationInvoiceStatus.Rejected;
        entity.RejectedAt = DateTimeOffset.UtcNow;
        entity.RejectionReason = command.Reason;

        ValuationInvoiceAuditTrail.Append(context, entity.ValuationInvoiceId,
            ValuationInvoiceEventType.Rejected, command.Reason, amountAfter: entity.Amount);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
