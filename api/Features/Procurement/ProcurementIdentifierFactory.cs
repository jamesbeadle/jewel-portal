namespace Jewel.JPMS.Api.Features.Procurement;

internal static class ProcurementIdentifierFactory
{
    private const string CompactGuidFormat = "N";
    public const string BidPackagePrefix = "BPI-";
    public const string WorkOrderPrefix = "WO-";

    /// <summary>Human reference for a bid package number, e.g. 1 => "BPI-0001".</summary>
    public static string BidPackageReference(int number) => $"{BidPackagePrefix}{number:0000}";

    /// <summary>Human reference for a work order number, e.g. 32 => "WO-0032".</summary>
    public static string WorkOrderReference(int number) => $"{WorkOrderPrefix}{number:0000}";

    public static string NextBidPackageId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextRecipientId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextLineItemId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextQuoteId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextWorkOrderId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextWorkOrderLineId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
