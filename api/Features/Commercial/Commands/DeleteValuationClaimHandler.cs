using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// Deletes a claim (any status) with its lines — the escape hatch for test claims and false
/// starts. Since 2026-09-18 the claim is also the statement behind its invoice, so a claim
/// with a live (non-cancelled) invoice against it is refused: cancel or delete the invoice
/// first, exactly as reopening demands. Cancelled invoices that named the claim keep their
/// money and history with the link cleared. The alias rows of any retired snapshot frozen
/// from the claim go with it (a JPMS/VRS-… tag on old mail then resolves to nothing — as a
/// deleted claim's JPMS/VAL-… tag always did). Note: deleting a Confirmed claim removes the
/// baseline later claims' period increments were measured against — those recompute on the
/// next entry edit.
/// </summary>
public sealed class DeleteValuationClaimHandler : ICommandHandler<DeleteValuationClaim, Acknowledgement>
{
    private readonly JpmsContext context;
    public DeleteValuationClaimHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeleteValuationClaim command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationClaims.FindAsync(new object?[] { command.ValuationClaimId }, cancellationToken);
        if (entity is null) return new Acknowledgement(command.ValuationClaimId); // already gone — idempotent

        var liveInvoiceReference = await context.ValuationInvoices.AsNoTracking()
            .Where(invoice => invoice.ValuationClaimId == entity.ValuationClaimId
                              && invoice.Status != (int)ValuationInvoiceStatus.Cancelled)
            .OrderByDescending(invoice => invoice.Number)
            .Select(invoice => invoice.Reference)
            .FirstOrDefaultAsync(cancellationToken);
        if (liveInvoiceReference is not null)
            throw new InvalidOperationException(
                $"This claim is the statement behind invoice {liveInvoiceReference} — cancel or delete that invoice first, then delete the claim.");

        var claimLines = await context.ClaimLines
            .Where(line => line.ValuationClaimId == command.ValuationClaimId)
            .ToListAsync(cancellationToken);
        context.ClaimLines.RemoveRange(claimLines);

        var cancelledInvoices = await context.ValuationInvoices
            .Where(invoice => invoice.ValuationClaimId == command.ValuationClaimId)
            .ToListAsync(cancellationToken);
        foreach (var invoice in cancelledInvoices) invoice.ValuationClaimId = null;

        var aliases = await context.ValuationClaimLegacyStatements
            .Where(alias => alias.ValuationClaimId == command.ValuationClaimId)
            .ToListAsync(cancellationToken);
        context.ValuationClaimLegacyStatements.RemoveRange(aliases);

        context.ValuationClaims.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.ValuationClaimId);
    }
}
