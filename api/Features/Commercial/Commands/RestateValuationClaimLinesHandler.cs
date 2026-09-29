using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// Moves money between a locked claim's own frozen rows. The rule is ClaimRestatement's: the
/// claim's total works complete and its contract-side works come out to the penny as they were,
/// or nothing is written. Only rows the claim already carries are restated — nothing is added
/// and nothing is re-copied from the live bill, because the statement the client holds is the
/// rows as frozen. "This period" is re-derived on the restated claim and on the claim
/// immediately after it (ClaimPeriodBaseline: its previous is this claim); the frozen footer is
/// untouched because its figures do not move.
/// </summary>
public sealed class RestateValuationClaimLinesHandler : ICommandHandler<RestateValuationClaimLines, IReadOnlyList<ClaimLine>>
{
    private readonly JpmsContext context;
    public RestateValuationClaimLinesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ClaimLine>> HandleAsync(RestateValuationClaimLines command, CancellationToken cancellationToken)
    {
        var claim = await context.ValuationClaims.FindAsync(new object?[] { command.ValuationClaimId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Valuation claim {command.ValuationClaimId} was not found.");
        if (claim.Status == (int)ValuationClaimStatus.Draft)
            throw new InvalidOperationException("A Draft claim's figures are set with \"Set % complete\" — only a locked claim is restated.");

        var rows = await RowsBeingRestatedAsync(claim, command.Entries, cancellationToken);
        var percentByLine = command.Entries.ToDictionary(entry => entry.ValuationLineItemId, entry => entry.PercentComplete);
        var outcome = ClaimRestatement.Plan(rows.Select(ToRestatementRow).ToList(), percentByLine);
        RefuseUnlessTheTotalHolds(claim, outcome);

        var previousByLine = await ClaimPeriodBaseline.PreviousCumulativeByLineAsync(
            context, claim.ProjectId, claim.ClaimNumber, cancellationToken);
        var rowsByLine = rows.ToDictionary(row => row.ValuationLineItemId);
        foreach (var entry in outcome.Entries)
        {
            var row = rowsByLine[entry.ValuationLineItemId];
            row.PercentComplete = entry.PercentComplete;
            row.CumulativeClaimed = entry.CumulativeClaimed;
            row.PeriodIncrement = ClaimPeriodBaseline.PeriodIncrement(entry.CumulativeClaimed, previousByLine, entry.ValuationLineItemId);
        }
        await RederiveNextClaimPeriodAsync(claim, rowsByLine, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        return rows.Select(row => row.ToModel()).ToList();
    }

    private async Task<List<ClaimLineEntity>> RowsBeingRestatedAsync(
        ValuationClaimEntity claim, IReadOnlyList<ClaimEntryInput> entries, CancellationToken cancellationToken)
    {
        var lineIds = entries.Select(entry => entry.ValuationLineItemId).ToList();
        var rows = await context.ClaimLines
            .Where(row => row.ValuationClaimId == claim.ValuationClaimId && lineIds.Contains(row.ValuationLineItemId))
            .ToListAsync(cancellationToken);
        var carried = rows.Select(row => row.ValuationLineItemId).ToHashSet(StringComparer.Ordinal);
        var missing = lineIds.FirstOrDefault(lineId => !carried.Contains(lineId));
        if (missing is not null)
            throw new KeyNotFoundException($"Valuation line item {missing} is not on this claim's statement — a locked claim only restates the lines it carries.");
        return rows;
    }

    private static ClaimRestatement.Row ToRestatementRow(ClaimLineEntity row) =>
        new(row.ValuationLineItemId, row.LineAmount,
            row.ToStatementLine().CountsTowardTotals,
            row.ElementType == (int)ValuationElementType.Variation,
            row.CumulativeClaimed);

    private static void RefuseUnlessTheTotalHolds(ValuationClaimEntity claim, ClaimRestatement.Outcome outcome)
    {
        if (outcome.KeepsTheTotal) return;
        var name = string.IsNullOrWhiteSpace(claim.Name) ? $"Claim {claim.ClaimNumber}" : claim.Name;
        if (outcome.TotalMoved != 0m)
            throw new InvalidOperationException(
                $"{name} is locked at {claim.TotalWorksComplete:N2} works complete — this restatement would move {outcome.TotalMoved:N2} in or out. Money moves between its lines, never in total.");
        throw new InvalidOperationException(
            $"{name} would keep its total but move {outcome.ContractSideMoved:N2} between contract works and variations, which changes the deposit release. Restate within one side at a time.");
    }

    /// <summary>The claim after this one measures "this period" against these rows, so its stored
    /// copy of the figure follows (readers derive it live; the stored value is what the report table shows).</summary>
    private async Task RederiveNextClaimPeriodAsync(
        ValuationClaimEntity claim, IReadOnlyDictionary<string, ClaimLineEntity> restatedByLine, CancellationToken cancellationToken)
    {
        var next = await context.ValuationClaims
            .Where(candidate => candidate.ProjectId == claim.ProjectId && candidate.ClaimNumber > claim.ClaimNumber)
            .OrderBy(candidate => candidate.ClaimNumber)
            .FirstOrDefaultAsync(cancellationToken);
        if (next is null) return;

        var lineIds = restatedByLine.Keys.ToList();
        var nextRows = await context.ClaimLines
            .Where(row => row.ValuationClaimId == next.ValuationClaimId && lineIds.Contains(row.ValuationLineItemId))
            .ToListAsync(cancellationToken);
        foreach (var row in nextRows)
        {
            var previous = restatedByLine[row.ValuationLineItemId];
            row.PeriodIncrement = row.CumulativeClaimed - previous.CumulativeClaimed;
        }
    }
}
