namespace Jewel.JPMS.Api.Features.Commercial;

internal static class CommercialIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextValuationId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextTimesheetId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextClaimPeriodId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextCostCodeBudgetId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextValuationLineItemId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextValuationClaimId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextClaimLineId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
