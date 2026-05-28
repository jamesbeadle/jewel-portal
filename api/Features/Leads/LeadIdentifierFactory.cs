namespace Jewel.JPMS.Api.Features.Leads;

internal static class LeadIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextLeadId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextSiteVisitId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextInfoChaseItemId() => Guid.NewGuid().ToString(CompactGuidFormat);
    public static string NextProposalId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
