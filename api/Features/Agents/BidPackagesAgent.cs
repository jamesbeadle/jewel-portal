using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Agents;

// Procurement. When implemented it will issue bid packages, select bids, raise purchase orders,
// and hand off to scheduling. Ships as a stub.
public sealed class BidPackagesAgent : StubAgent
{
    public const string AgentKey = "bid-packages";

    public override string Key => AgentKey;
    public override string DisplayName => "Bid Packages Agent";
    public override AgentDiscipline Discipline => AgentDiscipline.Procurement;
    public override IReadOnlyCollection<RecordType> AppliesTo => new[] { RecordType.BidPackageInvite };
    public override string Summary =>
        "Issues bid packages, selects bids, raises purchase orders, and hands off to scheduling.";
}
