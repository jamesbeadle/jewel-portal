using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Pins the rule behind the status pill's Approved pick (2026-09-23, Nigel: choosing Approved does
// what the approve button does, straight away): a staged build-up is the approval itself — the
// first line's cost centre as the primary, the lines' total as the value — and with nothing
// staged there is no approval to run, so the person is taken to the panel to enter the lines.
public sealed class VariationApprovalTests
{
    private static VariationOrder Issued(IReadOnlyList<VariationLineInput>? draftLines) => new(
        VariationOrderId: "vo-5",
        ProjectId: "proj-1",
        RequestId: "",
        Number: 5,
        Reference: "VOQ-0005",
        Title: "Plastering to remaining walls, ceilings and damp-affected areas",
        Description: "",
        Status: VariationOrderStatus.Issued,
        SelectedBidPackageId: null,
        SelectedSubcontractorId: null,
        EstimatedValue: 3_150m,
        VariationRef: null,
        Value: 0m,
        CostCode: null,
        CreatedAt: DateTimeOffset.UnixEpoch,
        CreatedByEmail: "qs@jewelbb.co.uk",
        IssuedAt: DateTimeOffset.UnixEpoch,
        DraftLines: draftLines);

    [Fact]
    public void A_staged_build_up_is_the_approval_the_panel_would_submit()
    {
        var staged = new[]
        {
            new VariationLineInput("INT-PLS", "A. Skim to hall walls", 60m, 35m),
            new VariationLineInput("INT-PLS", "B. Ceilings", 20m, 40m),
            new VariationLineInput("INT-DEC", "C. Mist coat", 1m, -50m),
        };

        var approval = VariationApproval.FromStagedBuildUp(Issued(staged));

        Assert.NotNull(approval);
        Assert.Equal("INT-PLS", approval!.PrimaryCostCode);
        Assert.Equal(2_850m, approval.Total);
        Assert.Same(staged, approval.Lines);
    }

    [Fact]
    public void Nothing_staged_is_no_approval_to_run()
    {
        Assert.Null(VariationApproval.FromStagedBuildUp(Issued(null)));
        Assert.Null(VariationApproval.FromStagedBuildUp(Issued(Array.Empty<VariationLineInput>())));
    }
}
