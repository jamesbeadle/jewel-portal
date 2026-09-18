using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

// "We are claiming this." The LOCK — and, since 2026-09-18, the statement: freezes the bill
// onto the claim's own rows (ValuationStatementLines.FreezeAsync — every line's description,
// code, quantity, rate, amount and client reference beside the % and money claimed), freezes
// the summary footer, and moves the claim from Draft to Preapproved, awaiting the client.
// From here the claim IS the report the invoice is raised against and the client is sent;
// no separate snapshot is taken at any later step.
public sealed class PreapproveValuationClaimHandler : ICommandHandler<PreapproveValuationClaim, ValuationClaim>
{
    private readonly JpmsContext context;
    private readonly AuditTrail audit;
    public PreapproveValuationClaimHandler(JpmsContext context, AuditTrail audit) { this.context = context; this.audit = audit; }

    public async Task<ValuationClaim> HandleAsync(PreapproveValuationClaim command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationClaims.FindAsync(new object?[] { command.ValuationClaimId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Valuation claim {command.ValuationClaimId} was not found.");

        await ValuationStatementLines.FreezeAsync(context, entity, cancellationToken);
        await ValuationClaimSummary.ApplyTotalsAsync(context, entity, cancellationToken);
        entity.Status = (int)ValuationClaimStatus.Preapproved;
        entity.PreapprovedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        // Audit (client-facing, after the save so the trail never records a freeze that didn't
        // commit): the locked statement is what a client can be shown.
        await audit.WriteAsync(
            AuditEventType.SnapshotTaken,
            $"Valuation statement frozen: {entity.ToModel().DisplayName} locked.",
            pathway: "Client",
            projectId: entity.ProjectId,
            recordReference: $"Claim {entity.ClaimNumber}",
            cancellationToken: cancellationToken);

        return entity.ToModel();
    }
}
