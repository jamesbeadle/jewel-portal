using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

/// <summary>
/// Raised/Rejected -> Cancelled. The invoice is kept for the audit trail but excluded from every
/// total; its snapshots are flagged superseded. Cancellation is only possible before issue, so
/// certified/paid totals are never touched.
/// </summary>
public sealed class CancelValuationInvoiceHandler : ICommandHandler<CancelValuationInvoice, ValuationInvoice>
{
    private readonly JpmsContext context;
    public CancelValuationInvoiceHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationInvoice> HandleAsync(CancelValuationInvoice command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationInvoices.FindAsync(new object[] { command.ValuationInvoiceId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Valuation invoice {command.ValuationInvoiceId} not found.");
        if (entity.Status is not ((int)ValuationInvoiceStatus.Raised or (int)ValuationInvoiceStatus.Rejected))
            throw new InvalidOperationException("Only a Raised or Rejected valuation invoice can be cancelled.");

        entity.Status = (int)ValuationInvoiceStatus.Cancelled;
        entity.CancelledAt = DateTimeOffset.UtcNow;

        var snapshots = await context.ValuationReportSnapshots
            .Where(snapshot => snapshot.ValuationInvoiceId == entity.ValuationInvoiceId && !snapshot.IsSuperseded)
            .ToListAsync(cancellationToken);
        foreach (var snapshot in snapshots) snapshot.IsSuperseded = true;

        ValuationInvoiceAuditTrail.Append(context, entity.ValuationInvoiceId,
            ValuationInvoiceEventType.Cancelled, command.Note ?? "", amountBefore: entity.Amount);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
