using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// The claim rows a bill edit may remove: a DRAFT claim's entries for the given bill lines. A
/// locked claim's rows are its frozen statement (2026-09-18) — they carry the bill line copied
/// by value and the money the client was asked for — and are never removed by a bill edit;
/// their ValuationLineItemId becomes provenance only.
/// </summary>
internal static class DraftClaimRows
{
    public static async Task<List<ClaimLineEntity>> ForLinesAsync(
        JpmsContext context, IReadOnlyCollection<string> lineItemIds, CancellationToken cancellationToken)
    {
        if (lineItemIds.Count == 0) return new List<ClaimLineEntity>();
        return await (
                from row in context.ClaimLines
                join claim in context.ValuationClaims on row.ValuationClaimId equals claim.ValuationClaimId
                where lineItemIds.Contains(row.ValuationLineItemId)
                      && claim.Status == (int)ValuationClaimStatus.Draft
                select row)
            .ToListAsync(cancellationToken);
    }
}
