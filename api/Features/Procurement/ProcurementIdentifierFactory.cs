namespace Jewel.JPMS.Api.Features.Procurement;

internal static class ProcurementIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextBidPackageId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextRecipientId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextLineItemId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextQuoteId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextWorkOrderId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
