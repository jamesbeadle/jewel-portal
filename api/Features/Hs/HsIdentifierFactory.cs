namespace Jewel.JPMS.Api.Features.Hs;

internal static class HsIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextHsRecordId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextHsRecordAttendanceId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
