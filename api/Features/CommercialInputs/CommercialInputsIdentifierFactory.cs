namespace Jewel.JPMS.Api.Features.CommercialInputs;

internal static class CommercialInputsIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextDayworkId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextContraChargeId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextSubcontractorRetentionId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
