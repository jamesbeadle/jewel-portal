using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

// "We are claiming this." Freezes the summary totals from the current entries and moves
// the claim from Draft to Preapproved, awaiting the client.
public sealed class PreapproveValuationClaimHandler : ICommandHandler<PreapproveValuationClaim, ValuationClaim>
{
    private readonly JpmsContext context;
    public PreapproveValuationClaimHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationClaim> HandleAsync(PreapproveValuationClaim command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationClaims.FindAsync(new object?[] { command.ValuationClaimId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Valuation claim {command.ValuationClaimId} was not found.");

        await ValuationClaimSummary.ApplyTotalsAsync(context, entity, cancellationToken);
        entity.Status = (int)ValuationClaimStatus.Preapproved;
        entity.PreapprovedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
