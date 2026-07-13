namespace Jewel.JPMS.Api.Features.Variations;

internal static class VariationsIdentifierFactory
{
    private const string CompactGuidFormat = "N";
    public const string VoqPrefix = "VOQ-";

    public static string NextVoqId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextBidPackageId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextVariationOrderId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextVariationRequestId() => Guid.NewGuid().ToString(CompactGuidFormat);

    // Ids for the cross-feature rows an approval writes (valuation line, QS accrual, budget).
    public static string NextValuationLineItemId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextQsAccrualId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextCostCodeBudgetId() => Guid.NewGuid().ToString(CompactGuidFormat);

    /// <summary>Human reference for a VOQ number, e.g. 1 => "VOQ-0001".</summary>
    public static string Reference(int number) => $"{VoqPrefix}{number:0000}";

    /// <summary>Human reference for a Variation Order number, e.g. 18 => "V18".</summary>
    public static string VariationRef(int number) => $"V{number}";
}
