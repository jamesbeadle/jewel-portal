using Jewel.JPMS.Api.Features.Commercial.Commands;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Line-level client references (2026-08-26): each valuation line can carry the client's own
// schedule-of-works item number ("1.03"), so the report matches the contract document line by
// line. The per-cost-centre map stays as the fallback; these tests pin the contract surface —
// older positional callers still compile, and the 64-character column limit is enforced at
// validation rather than surfacing as a database truncation error.
public sealed class ValuationLineClientReferenceTests
{
    private static AddValuationLineItem Add(string clientReference) => new(
        ProjectId: "P1",
        ElementType: ValuationElementType.ContractWorks,
        SectionCode: "", SectionName: "General Preliminaries",
        VariationRef: "", VariationTitle: "",
        LineType: ValuationLineType.Priced,
        CostCode: "PRELIMS-PMG", Description: "Site manager", Unit: "week",
        Quantity: 12m, Rate: 750m, Comments: "", DisplayOrder: 1,
        ClientReference: clientReference);

    private static UpdateValuationLineItem Update(string clientReference) => new(
        ValuationLineItemId: "L1",
        ElementType: ValuationElementType.ContractWorks,
        SectionCode: "", SectionName: "General Preliminaries",
        VariationRef: "", VariationTitle: "",
        LineType: ValuationLineType.Priced,
        CostCode: "PRELIMS-PMG", Description: "Site manager", Unit: "week",
        Quantity: 12m, Rate: 750m, Comments: "", DisplayOrder: 1,
        ClientReference: clientReference);

    [Fact]
    public void Commands_defaultToNoClientReference_soOlderCallersStillCompile()
    {
        var add = new AddValuationLineItem(
            "P1", ValuationElementType.ContractWorks, "", "", "", "",
            ValuationLineType.Priced, "PRELIMS-PMG", "Site manager", "week", 12m, 750m, "", 1);
        Assert.Equal("", add.ClientReference);

        var line = new ValuationLineItem(
            "L1", "P1", ValuationElementType.ContractWorks, "", "", "", "",
            ValuationLineType.Priced, "PRELIMS-PMG", "Site manager", "week", 12m, 750m, 9_000m, "", 1);
        Assert.Equal("", line.ClientReference);
    }

    [Fact]
    public void AddValidation_acceptsAReferenceUpToTheColumnLimit()
    {
        var outcome = new AddValuationLineItemValidation().Check(Add(new string('9', 64)));
        Assert.False(outcome.HasFailed);
    }

    [Fact]
    public void AddValidation_rejectsAReferenceLongerThanTheColumn()
    {
        var outcome = new AddValuationLineItemValidation().Check(Add(new string('9', 65)));
        Assert.True(outcome.HasFailed);
    }

    [Fact]
    public void UpdateValidation_rejectsAReferenceLongerThanTheColumn()
    {
        var outcome = new UpdateValuationLineItemValidation().Check(Update(new string('9', 65)));
        Assert.True(outcome.HasFailed);
    }
}
