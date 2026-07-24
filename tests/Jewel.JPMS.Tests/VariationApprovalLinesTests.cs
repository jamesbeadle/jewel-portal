using System.Collections.Generic;
using System.Linq;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Xunit;

// The maths behind the approve panel's line-item build-up: a variation's value is the sum of its
// priced lines (an omit nets negatively), and each cost centre commits its own share. These mirror
// exactly what ApproveVariationOrderHandler computes from the same VariationLineInput list.
public sealed class VariationApprovalLinesTests
{
    private static ValuationLineType LineTypeFor(decimal quantity, decimal rate) =>
        quantity * rate < 0m ? ValuationLineType.Omit : ValuationLineType.Priced;

    private static decimal Total(IEnumerable<VariationLineInput> lines) =>
        lines.Sum(l => ValuationCalculations.LineAmount(LineTypeFor(l.Quantity, l.Rate), l.Quantity, l.Rate));

    [Fact]
    public void ApproveVariationOrder_defaultsToNoLines_soLegacySingleValueStillApplies()
    {
        var command = new ApproveVariationOrder("vo-1", "SUP-DOR", "qs@jewelbb.co.uk");

        Assert.Null(command.Value);
        Assert.Null(command.Lines);
    }

    [Fact]
    public void BuildUp_sumsLines_andNetsAnOmit()
    {
        var lines = new List<VariationLineInput>
        {
            new("SUP-DOR", "Door supply", 1m, 1000m),   // 1000
            new("INT-RDR", "Rendering", 2m, 250m),      //  500
            new("SUP-DOR", "Remove ironmongery", 1m, -300m) // -300 (omit)
        };

        Assert.Equal(1200m, Total(lines));
    }

    [Fact]
    public void BuildUp_commitsEachCostCentreItsOwnShare()
    {
        var lines = new List<VariationLineInput>
        {
            new("SUP-DOR", "Door supply", 1m, 1000m),
            new("INT-RDR", "Rendering", 2m, 250m),
            new("SUP-DOR", "Remove ironmongery", 1m, -300m)
        };

        var perCentre = lines
            .GroupBy(l => l.CostCode)
            .ToDictionary(g => g.Key, g => Total(g));

        Assert.Equal(700m, perCentre["SUP-DOR"]);  // 1000 - 300
        Assert.Equal(500m, perCentre["INT-RDR"]);
    }
}
