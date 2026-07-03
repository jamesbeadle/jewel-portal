namespace Jewel.JPMS.Api.Features.Lads;

internal static class LadsIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string Next() => Guid.NewGuid().ToString(CompactGuidFormat);
}
