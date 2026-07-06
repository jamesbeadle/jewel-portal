namespace Jewel.JPMS.Api.Features.CostCenters;

internal static class CostCenterIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    // Seeded rows use readable ids (cc-jbb-*); rows added through the admin
    // page get a generated id. Only the Code is user-facing.
    public static string Next() => $"cc-{Guid.NewGuid().ToString(CompactGuidFormat)}";
}
