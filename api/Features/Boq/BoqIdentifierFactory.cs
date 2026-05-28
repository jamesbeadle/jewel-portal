namespace Jewel.JPMS.Api.Features.Boq;

internal static class BoqIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextBoqLineItemId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextBoqSignOffId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
