using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// Keeps every claim honest when an approved variation's breakdown is re-shaped underneath it —
/// lines re-priced, added or dropped in one revision (ReviseVariationOrderLinesHandler).
///
/// The rule lives in ClaimRespread (contracts): a claim locks the money it certified for the
/// variation, not the shape of the lines beneath it, so each claim's money is dealt back across
/// the revised lines — settled claims to the penny, the Draft at its own percentages — and "this
/// period" is re-derived claim by claim. Because the previous claim's rows are in the same change
/// tracker, not yet saved, the baseline is threaded through the plan rather than read from the
/// database (ClaimPeriodBaseline reads what is stored). Snapshots already frozen from a claim are
/// value copies and are not touched. Entities are mutated (and added) on the caller's change
/// tracker, not saved.
/// </summary>
internal static class VariationClaimRespread
{
    /// <summary>One claim entry as it stood BEFORE the revision, with the claim it belongs to.</summary>
    public sealed record PriorEntry(ClaimLineEntity Entry, string ValuationClaimId, int ClaimNumber, int Status);

    /// <summary>
    /// Re-spreads every claim's money for the variation across <paramref name="lines"/> — the
    /// variation's lines as they stand after the revision (re-priced rows mutated in place, added
    /// rows with their ids minted, dropped rows gone), <paramref name="addedLineIds"/> naming the
    /// added ones. <paramref name="priorEntries"/> is every entry that stood against the
    /// variation's lines before the revision, dropped lines included: a dropped line's Draft
    /// money is gone with it, and its settled money is always £0 (the handler refuses anything
    /// else). <paramref name="oldTotal"/> is the variation's value before the revision.
    /// </summary>
    public static async Task ApplyAsync(
        JpmsContext context,
        string projectId,
        IReadOnlyList<ValuationLineItemEntity> lines,
        IReadOnlySet<string> addedLineIds,
        IReadOnlyList<PriorEntry> priorEntries,
        decimal oldTotal,
        CancellationToken cancellationToken)
    {
        if (priorEntries.Count == 0 || lines.Count == 0) return;

        // Every claim on the project in number order: the baseline for claim N is the claim
        // immediately before it whether or not that claim carried the variation.
        var claims = (await context.ValuationClaims.AsNoTracking()
                .Where(claim => claim.ProjectId == projectId)
                .OrderBy(claim => claim.ClaimNumber)
                .Select(claim => new { claim.ValuationClaimId, claim.Status })
                .ToListAsync(cancellationToken))
            .Select(claim => new ClaimRespread.Claim(claim.ValuationClaimId, claim.Status == (int)ValuationClaimStatus.Draft))
            .ToList();

        var plan = ClaimRespread.Plan(
            claims,
            priorEntries.Select(row => new ClaimRespread.PriorEntry(
                row.ValuationClaimId, row.Entry.ValuationLineItemId, row.Entry.PercentComplete, row.Entry.CumulativeClaimed)).ToList(),
            lines.Select(line => (line.ValuationLineItemId, line.LineAmount)).ToList(),
            addedLineIds,
            oldTotal);

        var entities = priorEntries.ToDictionary(row => (row.ValuationClaimId, row.Entry.ValuationLineItemId), row => row.Entry);
        var lockedClaimIds = claims.Where(claim => !claim.IsDraft).Select(claim => claim.ValuationClaimId).ToHashSet(StringComparer.Ordinal);
        var linesById = lines.ToDictionary(line => line.ValuationLineItemId);
        var clientReferences = lockedClaimIds.Count == 0
            ? new Dictionary<string, string>()
            : await ValuationStatementLines.ClientReferencesByCostCodeAsync(context, projectId, cancellationToken);
        // Added rows on a locked claim join the frozen statement after its last line.
        var nextOrderByClaim = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var claimId in lockedClaimIds)
            nextOrderByClaim[claimId] = 1 + (await context.ClaimLines
                .Where(row => row.ValuationClaimId == claimId)
                .Select(row => (int?)row.DisplayOrder)
                .MaxAsync(cancellationToken) ?? -1);

        foreach (var entry in plan)
        {
            var locked = lockedClaimIds.Contains(entry.ValuationClaimId);
            if (!entry.IsNew && entities.TryGetValue((entry.ValuationClaimId, entry.ValuationLineItemId), out var entity))
            {
                entity.PercentComplete = entry.PercentComplete;
                entity.CumulativeClaimed = entry.CumulativeClaimed;
                entity.PeriodIncrement = entry.PeriodIncrement;
                if (locked && linesById.TryGetValue(entry.ValuationLineItemId, out var repriced))
                    ValuationStatementLines.CopyBillLine(entity, repriced, clientReferences);
                continue;
            }

            // A line the revision added: the claim carried the variation, so it carries the new
            // line too.
            var added = new ClaimLineEntity
            {
                ClaimLineId = CommercialIdentifierFactory.NextClaimLineId(),
                ValuationClaimId = entry.ValuationClaimId,
                ValuationLineItemId = entry.ValuationLineItemId,
                PercentComplete = entry.PercentComplete,
                CumulativeClaimed = entry.CumulativeClaimed,
                PeriodIncrement = entry.PeriodIncrement
            };
            if (locked && linesById.TryGetValue(entry.ValuationLineItemId, out var addedLine))
            {
                ValuationStatementLines.CopyBillLine(added, addedLine, clientReferences);
                added.DisplayOrder = nextOrderByClaim[entry.ValuationClaimId]++;
            }
            context.ClaimLines.Add(added);
        }
    }
}
