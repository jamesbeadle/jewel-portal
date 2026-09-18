using Jewel.JPMS.Contracts.ValuationInvoices;

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

        // The statement stays with the locked claim (2026-09-18); cancelling frees the claim to be
        // reopened, re-locked and re-raised, and the retired snapshot aliases just note the invoice.
        var aliases = await context.ValuationClaimLegacyStatements
            .Where(alias => alias.ValuationInvoiceId == entity.ValuationInvoiceId && !alias.IsSuperseded)
            .ToListAsync(cancellationToken);
        foreach (var alias in aliases) alias.IsSuperseded = true;

        ValuationInvoiceAuditTrail.Append(context, entity.ValuationInvoiceId,
            ValuationInvoiceEventType.Cancelled, command.Note ?? "", amountBefore: entity.Amount);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
