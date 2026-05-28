namespace Jewel.JPMS.Api.Features.Changes;

internal static class ChangesIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string Next() => Guid.NewGuid().ToString(CompactGuidFormat);
}
