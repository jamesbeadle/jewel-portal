using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Guarantees behind the "Add variation manually" entry point: a standalone variation order created
// with no request, optionally carrying a caller-set number so it can be matched to a reference
// already issued to the client. The handler-side numbering / duplicate guard lives in the API; these
// are the contract- and model-level shapes the UI relies on.
public sealed class ManualVariationOrderTests
{
    [Fact]
    public void CreateManualVariationOrder_defaultsAreEmpty_soCallersOptIn()
    {
        var command = new CreateManualVariationOrder("proj-1", "qs@jewelbb.co.uk", "Kitchen island stone swap");

        Assert.Null(command.Number);          // auto-assign the project's next number
        Assert.Null(command.Description);
        Assert.Null(command.EstimatedValue);
        // The document's narrative sections are optional at creation — editable at every stage.
        Assert.Null(command.CommercialBasis);
        Assert.Null(command.ProgrammeImpact);
        Assert.Null(command.Exclusions);
    }

    [Fact]
    public void CreateManualVariationOrder_carriesACallerSetNumber()
    {
        var command = new CreateManualVariationOrder("proj-1", "qs@jewelbb.co.uk", "Rooflight upgrade")
            with { Number = 50, EstimatedValue = 12_500m };

        Assert.Equal(50, command.Number);
        Assert.Equal(12_500m, command.EstimatedValue);
    }

    [Fact]
    public void StandaloneVariation_hasNoRequest_andRendersItsNumber()
    {
        // The shape the manual handler persists: empty RequestId (the register's "No request" badge)
        // and a Number that renders "V50" — one number for one document, shown the same way at every
        // stage. The stored Reference keeps its historic "VOQ-0050" spelling because it is a
        // persisted identifier, never UI copy (see CLAUDE.md), so the two deliberately differ.
        var order = new VariationOrder(
            VariationOrderId: "vo-1",
            ProjectId: "proj-1",
            RequestId: "",
            Number: 50,
            Reference: "VOQ-0050",
            Title: "Rooflight upgrade",
            Description: "",
            Status: VariationOrderStatus.Quoting,
            SelectedBidPackageId: null,
            SelectedSubcontractorId: null,
            EstimatedValue: 12_500m,
            VariationRef: null,
            Value: 0m,
            CostCode: null,
            CreatedAt: DateTimeOffset.UnixEpoch,
            CreatedByEmail: "qs@jewelbb.co.uk");

        Assert.True(string.IsNullOrWhiteSpace(order.RequestId));   // unlinked → "No request" in the register
        Assert.Equal("VOQ-0050", order.Reference);                 // persisted identifier, unchanged
        Assert.Equal("V50", order.DisplayNumber);                  // what a user actually reads
        Assert.Equal(VariationOrderStatus.Quoting, order.Status); // a draft until approved
    }
}
