using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

// Batched RecordClaimEntry: upserts many lines' % complete on one claim in a single save.
// Same maths per line as the single-entry handler — cumulative = % x line amount, period
// increment = cumulative minus the cumulative last confirmed for that line — with the
// baselines fetched once for the whole batch rather than per line.
public sealed class RecordClaimEntriesHandler : ICommandHandler<RecordClaimEntries, IReadOnlyList<ClaimLine>>
{
    private readonly JpmsContext context;
    public RecordClaimEntriesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ClaimLine>> HandleAsync(RecordClaimEntries command, CancellationToken cancellationToken)
    {
        var claim = await context.ValuationClaims.FindAsync(new object?[] { command.ValuationClaimId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Valuation claim {command.ValuationClaimId} was not found.");

        var lineAmountById = await context.ValuationLineItems
            .Where(line => line.ProjectId == claim.ProjectId)
            .ToDictionaryAsync(line => line.ValuationLineItemId, line => line.LineAmount, cancellationToken);

        // Cumulative claimed per line at the most recent Confirmed claim before this one —
        // the same rule as RecordClaimEntryHandler, fetched once for the whole batch.
        var baselineByLine = (await (
                from claimLine in context.ClaimLines
                join priorClaim in context.ValuationClaims on claimLine.ValuationClaimId equals priorClaim.ValuationClaimId
                where priorClaim.ProjectId == claim.ProjectId
                      && priorClaim.Status == (int)ValuationClaimStatus.Confirmed
                      && priorClaim.ClaimNumber < claim.ClaimNumber
                select new { claimLine.ValuationLineItemId, claimLine.CumulativeClaimed, priorClaim.ClaimNumber })
                .ToListAsync(cancellationToken))
            .GroupBy(entry => entry.ValuationLineItemId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(entry => entry.ClaimNumber).First().CumulativeClaimed);

        var existingByLine = await context.ClaimLines
            .Where(line => line.ValuationClaimId == command.ValuationClaimId)
            .ToDictionaryAsync(line => line.ValuationLineItemId, cancellationToken);

        var results = new List<ClaimLineEntity>(command.Entries.Count);
        foreach (var input in command.Entries)
        {
            if (!lineAmountById.TryGetValue(input.ValuationLineItemId, out var lineAmount))
                throw new KeyNotFoundException($"Valuation line item {input.ValuationLineItemId} was not found on this project.");

            var cumulativeClaimed = ValuationCalculations.CumulativeClaimed(input.PercentComplete, lineAmount);
            var previousCumulative = baselineByLine.TryGetValue(input.ValuationLineItemId, out var confirmed) ? confirmed : 0m;

            if (!existingByLine.TryGetValue(input.ValuationLineItemId, out var entity))
            {
                entity = new ClaimLineEntity
                {
                    ClaimLineId = CommercialIdentifierFactory.NextClaimLineId(),
                    ValuationClaimId = command.ValuationClaimId,
                    ValuationLineItemId = input.ValuationLineItemId
                };
                context.ClaimLines.Add(entity);
                existingByLine[input.ValuationLineItemId] = entity;
            }

            entity.PercentComplete = input.PercentComplete;
            entity.CumulativeClaimed = cumulativeClaimed;
            entity.PeriodIncrement = cumulativeClaimed - previousCumulative;
            results.Add(entity);
        }

        await context.SaveChangesAsync(cancellationToken);
        return results.Select(entity => entity.ToModel()).ToList();
    }
}
