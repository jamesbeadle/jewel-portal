using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

// Client has paid. Re-freezes the totals and marks the claim Confirmed; from here its
// per-row claimed amounts are final and advance CertifiedToDate for the next claim.
//
// Confirming also re-bases every LATER Draft claim on this one. A draft's per-line period
// increment is stored when its % is entered, measured against the latest CONFIRMED claim
// at that moment — so a September draft started while August was still only issued was
// measured against July. Confirming August is what makes it the baseline; without this
// pass the draft would keep carrying August's movement as its own until every line was
// re-saved. Preapproved later claims are left alone: their figures are frozen.
public sealed class ConfirmValuationClaimHandler : ICommandHandler<ConfirmValuationClaim, ValuationClaim>
{
    private readonly JpmsContext context;
    public ConfirmValuationClaimHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationClaim> HandleAsync(ConfirmValuationClaim command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationClaims.FindAsync(new object?[] { command.ValuationClaimId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Valuation claim {command.ValuationClaimId} was not found.");

        await ValuationClaimSummary.ApplyTotalsAsync(context, entity, cancellationToken);
        entity.Status = (int)ValuationClaimStatus.Confirmed;
        entity.ConfirmedAt = DateTimeOffset.UtcNow;
        if (entity.PreapprovedAt is null) entity.PreapprovedAt = entity.ConfirmedAt;

        await RebaseLaterDraftsAsync(entity.ProjectId, entity.ClaimNumber, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    // Same baseline rule as RecordClaimEntries / StartValuationClaim / DraftClaimRebase:
    // a line's increment on a draft = its cumulative − its cumulative at the most recent
    // Confirmed claim with a lower number. The claim being confirmed is already Confirmed
    // on the tracker here, so it takes part as a baseline.
    private async Task RebaseLaterDraftsAsync(string projectId, int confirmedClaimNumber, CancellationToken cancellationToken)
    {
        var laterDrafts = await context.ValuationClaims
            .Where(claim => claim.ProjectId == projectId
                            && claim.ClaimNumber > confirmedClaimNumber
                            && claim.Status == (int)ValuationClaimStatus.Draft)
            .Select(claim => new { claim.ValuationClaimId, claim.ClaimNumber })
            .ToListAsync(cancellationToken);
        if (laterDrafts.Count == 0) return;

        var draftIds = laterDrafts.Select(draft => draft.ValuationClaimId).ToList();
        var draftEntries = await context.ClaimLines
            .Where(line => draftIds.Contains(line.ValuationClaimId))
            .ToListAsync(cancellationToken);
        if (draftEntries.Count == 0) return;

        // Every Confirmed claim's entries on this project, by line, newest claim first —
        // read once and reused for each draft. The just-confirmed claim's rows are among
        // them: its status is Confirmed in the tracker and the query below is answered
        // from the database, so filter by claim number against the local list instead.
        var confirmedClaims = await context.ValuationClaims
            .Where(claim => claim.ProjectId == projectId
                            && (claim.Status == (int)ValuationClaimStatus.Confirmed
                                || claim.ClaimNumber == confirmedClaimNumber))
            .Select(claim => new { claim.ValuationClaimId, claim.ClaimNumber })
            .ToListAsync(cancellationToken);
        var confirmedNumberByClaim = confirmedClaims.ToDictionary(claim => claim.ValuationClaimId, claim => claim.ClaimNumber);
        var confirmedIds = confirmedNumberByClaim.Keys.ToList();

        var confirmedEntries = (await context.ClaimLines
                .Where(line => confirmedIds.Contains(line.ValuationClaimId))
                .Select(line => new { line.ValuationClaimId, line.ValuationLineItemId, line.CumulativeClaimed })
                .ToListAsync(cancellationToken))
            .Select(line => (line.ValuationLineItemId, ClaimNumber: confirmedNumberByClaim[line.ValuationClaimId], line.CumulativeClaimed))
            .ToLookup(line => line.ValuationLineItemId);

        var draftNumberByClaim = laterDrafts.ToDictionary(draft => draft.ValuationClaimId, draft => draft.ClaimNumber);
        foreach (var entry in draftEntries)
        {
            var draftNumber = draftNumberByClaim[entry.ValuationClaimId];
            var baseline = confirmedEntries[entry.ValuationLineItemId]
                .Where(prior => prior.ClaimNumber < draftNumber)
                .OrderByDescending(prior => prior.ClaimNumber)
                .Select(prior => prior.CumulativeClaimed)
                .FirstOrDefault();
            entry.PeriodIncrement = entry.CumulativeClaimed - baseline;
        }
    }
}
