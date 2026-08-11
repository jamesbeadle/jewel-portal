namespace Jewel.JPMS.Api.Features.UsefulInformation;

internal static class UsefulInformationIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string Next() => Guid.NewGuid().ToString(CompactGuidFormat);
}
