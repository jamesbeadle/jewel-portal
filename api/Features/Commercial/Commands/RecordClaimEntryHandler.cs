using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

// Upserts the % complete for one line within a Draft claim and recomputes the line's
// cumulative claimed amount and this period's increment (cumulative minus the cumulative
// last confirmed for the same line).
public sealed class RecordClaimEntryHandler : ICommandHandler<RecordClaimEntry, ClaimLine>
{
    private readonly JpmsContext context;
    public RecordClaimEntryHandler(JpmsContext context) { this.context = context; }

    public async Task<ClaimLine> HandleAsync(RecordClaimEntry command, CancellationToken cancellationToken)
    {
        var claim = await context.ValuationClaims.FindAsync(new object?[] { command.ValuationClaimId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Valuation claim {command.ValuationClaimId} was not found.");
        var lineItem = await context.ValuationLineItems.FindAsync(new object?[] { command.ValuationLineItemId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Valuation line item {command.ValuationLineItemId} was not found.");

        var cumulativeClaimed = ValuationCalculations.CumulativeClaimed(command.PercentComplete, lineItem.LineAmount);

        // Cumulative claimed on this line at the most recent confirmed claim before this one.
        var previousCumulative = await (
            from claimLine in context.ClaimLines
            join priorClaim in context.ValuationClaims on claimLine.ValuationClaimId equals priorClaim.ValuationClaimId
            where claimLine.ValuationLineItemId == command.ValuationLineItemId
                  && priorClaim.ProjectId == claim.ProjectId
                  && priorClaim.Status == (int)ValuationClaimStatus.Confirmed
                  && priorClaim.ClaimNumber < claim.ClaimNumber
            orderby priorClaim.ClaimNumber descending
            select (decimal?)claimLine.CumulativeClaimed)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;

        var entity = await context.ClaimLines.FirstOrDefaultAsync(
            line => line.ValuationClaimId == command.ValuationClaimId && line.ValuationLineItemId == command.ValuationLineItemId,
            cancellationToken);
        if (entity is null)
        {
            entity = new ClaimLineEntity
            {
                ClaimLineId = CommercialIdentifierFactory.NextClaimLineId(),
                ValuationClaimId = command.ValuationClaimId,
                ValuationLineItemId = command.ValuationLineItemId
            };
            context.ClaimLines.Add(entity);
        }

        entity.PercentComplete = command.PercentComplete;
        entity.CumulativeClaimed = cumulativeClaimed;
        entity.PeriodIncrement = cumulativeClaimed - previousCumulative;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
