namespace Jewel.JPMS.Api.Features.Subcontractors;

internal static class SubcontractorIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextSubcontractorId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextComplianceDocumentId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
