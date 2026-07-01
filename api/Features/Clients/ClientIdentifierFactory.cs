namespace Jewel.JPMS.Api.Features.Clients;

internal static class ClientIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextClientId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
