using Jewel.JPMS.Api.Features.Procurement.Commands;
using Jewel.JPMS.Contracts.Procurement;
using Xunit;

namespace Jewel.JPMS.Tests;

// A work-order line may be £0 (2026-09-15, the accountant's On The Level order): the supplier's
// quote listed the outlet gullies at 0.00, included under the formers, and the purchase order
// must list what the quote lists. Every raise path used to refuse zero as "not entered"; these
// pin that all three accept it, while a line still needs a cost centre and a title.
public sealed class WorkOrderNoChargeLineTests
{
    private const string RaisedBy = "accounts@jewelgroup.co.uk";

    private static readonly ManualWorkOrderLine Formers =
        new("SUP-SAN", "Bespoke 30MM former for LA", 2_970.40m, Quantity: 2m, Unit: "nr", UnitCost: 1_485.20m);

    private static readonly ManualWorkOrderLine GulliesAtNoCharge =
        new("SUP-SAN", "LA White 2-part horizontal outlet gully", 0m, Quantity: 2m, Unit: "nr", UnitCost: 0m);

    [Fact]
    public void CreateManualWorkOrder_acceptsALineAtNoCharge()
    {
        var outcome = new CreateManualWorkOrderValidation().Check(new CreateManualWorkOrder(
            "PRJ", "SUB", "Bespoke level-access shower formers, 2 off", "", RaisedBy,
            new[] { Formers, GulliesAtNoCharge }));

        Assert.False(outcome.HasFailed);
    }

    [Fact]
    public void UpdateManualWorkOrder_acceptsALineAtNoCharge()
    {
        var outcome = new UpdateManualWorkOrderValidation().Check(new UpdateManualWorkOrder(
            "PRJ", "WO", "SUB", "Bespoke level-access shower formers, 2 off", "",
            new[]
            {
                new UpdatedManualWorkOrderLine("WOL-1", Formers.CostCode, Formers.Title, Formers.Amount),
                new UpdatedManualWorkOrderLine(null, GulliesAtNoCharge.CostCode, GulliesAtNoCharge.Title, 0m,
                    Quantity: 2m, Unit: "nr", UnitCost: 0m)
            }));

        Assert.False(outcome.HasFailed);
    }

    [Fact]
    public void CreateWorkOrderFromMessage_acceptsALineAtNoCharge()
    {
        var outcome = new CreateWorkOrderFromMessageValidation().Check(new CreateWorkOrderFromMessage(
            "MSG", "PRJ", "SUB", "Bespoke level-access shower formers, 2 off", "",
            new[] { Formers, GulliesAtNoCharge },
            RaisedByEmail: RaisedBy));

        Assert.False(outcome.HasFailed);
    }

    [Fact]
    public void ALineAtNoCharge_stillNeedsACostCentreAndATitle()
    {
        var outcome = new CreateManualWorkOrderValidation().Check(new CreateManualWorkOrder(
            "PRJ", "SUB", "Bespoke level-access shower formers, 2 off", "", RaisedBy,
            new[] { new ManualWorkOrderLine("", "", 0m) }));

        Assert.True(outcome.HasFailed);
        Assert.Contains("Every line needs a cost centre.", outcome.Errors);
        Assert.Contains("Every line needs a title.", outcome.Errors);
    }
}
