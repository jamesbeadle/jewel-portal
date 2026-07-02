namespace Jewel.JPMS.Api.Features.Architects;

internal static class ArchitectIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextArchitectId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
