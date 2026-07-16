namespace Jewel.JPMS.Api.Features.Commercial;

internal static class CommercialIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextValuationId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextTimesheetId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextClaimPeriodId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextCostCodeBudgetId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextCostCentreCostProgressId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextCostCentreGroupId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextCostCentreGroupMemberId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextValuationLineItemId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextValuationClaimId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextClaimLineId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextValuationReportSnapshotId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextValuationReportSnapshotLineId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextXeroLineWorkOrderLinkId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextReconciliationPackageId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextReconciliationPackageOrderId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextReconciliationPackageSalesLineId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextReconciliationPackageCostLineId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
