using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// Jeremy, 2026-09-24: WO-0054 was By France's JP Air Conditioning order AND JBB-2026-005's
// painting order. Numbers stay per project, and every reference the portal shows or sends carries
// the project, so a number points at exactly one order; the short form is still read.
public sealed class WorkOrderReferenceTests
{
    private const string ByFrance = "JBB-2026-001";
    private const string PropertyServe = "JBB-2026-005";

    [Fact]
    public void TheSameNumberOnTwoProjects_readsAsTwoReferences()
    {
        Assert.Equal("JBB-2026-001-WO-0054", OrderWith(54, WorkOrderStatus.Released, ByFrance).Reference);
        Assert.Equal("JBB-2026-005-WO-0054", OrderWith(54, WorkOrderStatus.Released, PropertyServe).Reference);
    }

    [Fact]
    public void AProjectWithNoReference_leavesTheShortForm()
    {
        Assert.Equal("WO-0054", OrderWith(54, WorkOrderStatus.Released, "").Reference);
        Assert.Equal("WO-0054", WorkOrderReferences.Qualified("  ", 54));
    }

    [Fact]
    public void ADraft_stillReadsDraft_withNoProjectOrNumber()
    {
        Assert.Equal("Draft", OrderWith(0, WorkOrderStatus.Draft, ByFrance).Reference);
        Assert.Equal("Rejected", OrderWith(0, WorkOrderStatus.Rejected, ByFrance).Reference);
    }

    [Fact]
    public void TheEntity_keepsTheShortStemForTags_andReadsQualifiedForPeople()
    {
        var order = new WorkOrderEntity { WorkOrderId = "wo-bf-54", ProjectId = "p-bf", Number = 54 };

        Assert.Equal("WO-0054", order.Reference);
        Assert.Equal("JBB-2026-001-WO-0054", order.ReferenceOn(ByFrance));
    }

    [Fact]
    public void TheStatement_carriesTheProjectOnEachOrder()
    {
        var order = new SubcontractorStatementOrder("wo", 54, "Painting", WorkOrderStatus.Released, default, 843m, 843m,
            Array.Empty<SubcontractorStatementInvoice>(), PropertyServe);

        Assert.Equal("JBB-2026-005-WO-0054", order.Reference);
    }

    [Theory]
    [InlineData("JBB-2026-001-WO-0054", ByFrance, 54)]
    [InlineData("jbb-2026-005-wo-0054", "jbb-2026-005", 54)]
    [InlineData("JBB-2026-001 WO 54", ByFrance, 54)]
    [InlineData("WO-0054", null, 54)]
    [InlineData("wo54", null, 54)]
    [InlineData("54", null, 54)]
    public void AReference_isReadHoweverItIsSaid(string said, string? project, int number)
    {
        Assert.True(WorkOrderReferences.TryRead(said, out var readProject, out var readNumber));
        Assert.Equal((project, number), (readProject, readNumber));
    }

    [Theory]
    [InlineData("")]
    [InlineData("JBB-2026-001")]
    [InlineData("TWO-0054")]
    [InlineData("RFI-054")]
    [InlineData("WO-0000")]
    public void WhatIsNotAWorkOrder_isNotRead(string said) =>
        Assert.False(WorkOrderReferences.TryRead(said, out _, out _));

    private static WorkOrder OrderWith(int number, WorkOrderStatus status, string projectReference) =>
        new(WorkOrderId: "abcdef12-3456", ProjectId: "PRJ", BidPackageId: null,
            SubcontractorId: "S", Value: 1_000m, Scope: "", AwardedAt: default,
            AwardedByEmail: "", Number: number, Title: "Painting", Status: status,
            CreatedAt: default, ScheduledCompletion: null, ProjectReference: projectReference);
}
