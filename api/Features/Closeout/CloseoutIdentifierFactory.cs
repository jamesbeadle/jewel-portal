namespace Jewel.JPMS.Api.Features.Closeout;

internal static class CloseoutIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextDefectId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextSettlementRecordId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextVatAnalysisId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextRetentionReleaseId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
