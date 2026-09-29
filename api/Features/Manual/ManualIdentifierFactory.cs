namespace Jewel.JPMS.Api.Features.Manual;

internal static class ManualIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextModuleId() => Guid.NewGuid().ToString(CompactGuidFormat);

    public static string NextVersionId() => Guid.NewGuid().ToString(CompactGuidFormat);

    public static string NextAcknowledgementId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
