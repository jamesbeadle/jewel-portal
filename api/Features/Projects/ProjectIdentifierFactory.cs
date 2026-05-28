namespace Jewel.JPMS.Api.Features.Projects;

internal static class ProjectIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string Next() => Guid.NewGuid().ToString(CompactGuidFormat);
}
