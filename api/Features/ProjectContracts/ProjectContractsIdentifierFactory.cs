namespace Jewel.JPMS.Api.Features.ProjectContracts;

internal static class ProjectContractsIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextProjectContractId() => Guid.NewGuid().ToString(CompactGuidFormat);

    public static string NextProjectContractAmendmentId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
