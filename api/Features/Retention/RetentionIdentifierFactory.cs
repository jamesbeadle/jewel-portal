namespace Jewel.JPMS.Api.Features.Retention;

internal static class RetentionIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextProjectRetentionId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
