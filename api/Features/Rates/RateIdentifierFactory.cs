namespace Jewel.JPMS.Api.Features.Rates;

internal static class RateIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string Next() => Guid.NewGuid().ToString(CompactGuidFormat);
}
