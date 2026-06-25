namespace Jewel.JPMS.Api.Features.Requests;

internal static class RequestsIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string Next() => Guid.NewGuid().ToString(CompactGuidFormat);
}
