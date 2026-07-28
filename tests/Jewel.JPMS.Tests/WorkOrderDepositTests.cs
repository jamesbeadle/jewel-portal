using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Deposit + payment terms on the purchase order: the deposit is recorded as a percentage of
// the order value only (never a £ figure) and prints at the foot of the PO; payment terms
// live on the subcontractor record, defaulting to 30 days. These are the contract- and
// model-level shapes the UI relies on — chiefly that existing callers are untouched by the
// new optional parameters, and that UpdateSubcontractor's null terms mean "leave unchanged".
public sealed class WorkOrderDepositTests
{
    [Fact]
    public void CreateManualWorkOrder_defaultsToNoDeposit_soExistingCallersAreUntouched()
    {
        var command = new CreateManualWorkOrder(
            "PRJ", "SUB", "Groundworks", "", "fd@jewelgroup.co.uk",
            new[] { new ManualWorkOrderLine("100", "Dig", 1_000m) });

        Assert.False(command.DepositRequired);
        Assert.Null(command.DepositPercent);
    }

    [Fact]
    public void UpdateManualWorkOrder_defaultsToNoDeposit_soExistingCallersAreUntouched()
    {
        var command = new UpdateManualWorkOrder(
            "PRJ", "WO", "SUB", "Groundworks", "",
            new[] { new UpdatedManualWorkOrderLine(null, "100", "Dig", 1_000m) });

        Assert.False(command.DepositRequired);
        Assert.Null(command.DepositPercent);
    }

    [Fact]
    public void Subcontractor_defaultsToThirtyDayTerms()
    {
        var record = new Subcontractor(
            "S", "Brick Co", Array.Empty<Trade>(), "", "", "", "", default);

        Assert.Equal(30, record.PaymentTermsDays);
    }

    [Fact]
    public void UpdateSubcontractor_defaultsTermsToNull_meaningLeaveUnchanged()
    {
        var command = new UpdateSubcontractor(
            "S", "Brick Co", Array.Empty<string>(), "", "", "", "");

        Assert.Null(command.PaymentTermsDays);
    }

    [Fact]
    public void AddSubcontractorToDirectory_startsEveryCompanyOnThirtyDayTerms()
    {
        var command = new AddSubcontractorToDirectory(
            "Brick Co", Array.Empty<string>(), "", "", "", "");

        Assert.Equal(30, command.PaymentTermsDays);
    }
}
