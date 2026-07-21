using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// Deletes a claim (any status) and its per-line entries — the escape hatch for test
/// claims and false starts. Valuation invoices and report snapshots that referenced the
/// claim survive with the link cleared: what was invoiced/certified is history and must
/// not move because a claim was removed. Note: deleting a Confirmed claim removes the
/// baseline later claims' period increments were measured against — those recompute on
/// the next entry edit.
/// </summary>
public sealed class DeleteValuationClaimHandler : ICommandHandler<DeleteValuationClaim, Acknowledgement>
{
    private readonly JpmsContext context;
    public DeleteValuationClaimHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeleteValuationClaim command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationClaims.FindAsync(new object?[] { command.ValuationClaimId }, cancellationToken);
        if (entity is null) return new Acknowledgement(command.ValuationClaimId); // already gone — idempotent

        var claimLines = await context.ClaimLines
            .Where(line => line.ValuationClaimId == command.ValuationClaimId)
            .ToListAsync(cancellationToken);
        context.ClaimLines.RemoveRange(claimLines);

        // Invoices drawn against this claim keep their money and history — just unlink.
        var linkedInvoices = await context.ValuationInvoices
            .Where(invoice => invoice.ValuationClaimId == command.ValuationClaimId)
            .ToListAsync(cancellationToken);
        foreach (var invoice in linkedInvoices) invoice.ValuationClaimId = null;

        // Snapshots are immutable records of what was reported — keep them, clear the link.
        var linkedSnapshots = await context.ValuationReportSnapshots
            .Where(snapshot => snapshot.ValuationClaimId == command.ValuationClaimId)
            .ToListAsync(cancellationToken);
        foreach (var snapshot in linkedSnapshots) snapshot.ValuationClaimId = null;

        context.ValuationClaims.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.ValuationClaimId);
    }
}
