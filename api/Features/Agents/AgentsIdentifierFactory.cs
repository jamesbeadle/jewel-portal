namespace Jewel.JPMS.Api.Features.Agents;

internal static class AgentsIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string Next() => Guid.NewGuid().ToString(CompactGuidFormat);
}
