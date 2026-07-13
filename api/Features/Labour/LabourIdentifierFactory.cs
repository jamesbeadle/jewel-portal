namespace Jewel.JPMS.Api.Features.Labour;

internal static class LabourIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextWorkerId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextWorkerRateHistoryId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextProjectWorkerAssignmentId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextSiteAttendanceId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextXeroLineTimesheetCoverId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextLabourSettlementVarianceId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
