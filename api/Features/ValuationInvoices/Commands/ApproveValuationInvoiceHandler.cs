using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// Submitted -> Approved. Records the client's approval; the amount still doesn't count toward
/// "Certified to date" until the invoice is issued.
/// </summary>
public sealed class ApproveValuationInvoiceHandler : ICommandHandler<ApproveValuationInvoice, ValuationInvoice>
{
    private readonly JpmsContext context;
    public ApproveValuationInvoiceHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationInvoice> HandleAsync(ApproveValuationInvoice command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationInvoices.FindAsync(new object[] { command.ValuationInvoiceId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Valuation invoice {command.ValuationInvoiceId} not found.");
        if (entity.Status != (int)ValuationInvoiceStatus.Submitted)
            throw new InvalidOperationException("Only a Submitted valuation invoice can be approved.");

        entity.Status = (int)ValuationInvoiceStatus.Approved;
        entity.ApprovedAt = DateTimeOffset.UtcNow;

        ValuationInvoiceAuditTrail.Append(context, entity.ValuationInvoiceId,
            ValuationInvoiceEventType.Approved, command.Note ?? "", amountAfter: entity.Amount);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
