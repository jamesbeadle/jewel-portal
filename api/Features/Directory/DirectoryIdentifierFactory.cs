namespace Jewel.JPMS.Api.Features.Directory;

internal static class DirectoryIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextRoleId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
