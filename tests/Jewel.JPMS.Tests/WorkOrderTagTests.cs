using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Procurement;
using Jewel.JPMS.Api.Features.Procurement.Commands;
using Jewel.JPMS.Api.Features.RecordLinks.Providers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The 2026-09-14 Coombe Lane collision: work-order numbers are per project (By France's manual
// WO-0045 and Coombe Lane's migrated PO-45 share the number), so the mailbox tag carries the
// project and the flat legacy tag never resolves to a guess.
public sealed class WorkOrderTagTests
{
    private const string ByFrance = "P-BF";
    private const string CoombeLane = "P-CL";

    [Fact]
    public void Stem_isProjectQualified_andParsesBack()
    {
        Assert.Equal("JBB-2026-001-WO-0045", WorkOrderTags.Stem("JBB-2026-001", ByFrance, "WO-0045"));
        Assert.Equal("P-BF-WO-0045", WorkOrderTags.Stem(null, ByFrance, "WO-0045"));
        Assert.Equal(45, WorkOrderTags.NumberOf("JBB-2026-001-WO-0045"));
        Assert.Equal(45, WorkOrderTags.NumberOf("WO-0045"));
        Assert.Null(WorkOrderTags.NumberOf("JBB-2026-001-RFI-012"));
        Assert.Null(WorkOrderTags.NumberOf("TWO-0045"));
        Assert.True(WorkOrderTags.IsLegacyStem("WO-0045"));
        Assert.False(WorkOrderTags.IsLegacyStem("JBB-2026-001-WO-0045"));
    }

    [Fact]
    public async Task Provider_resolvesTheQualifiedStemToItsProject_andNeverGuessesALegacyCollision()
    {
        await using var context = await ContextWithTheCollisionAsync();
        var provider = new WorkOrderLinkProvider(context);

        var byFrance = await provider.FindByTagAsync("JBB-2026-001-WO-0045", CancellationToken.None);
        var coombeLane = await provider.FindByTagAsync("JBB-2026-005-WO-0045", CancellationToken.None);
        Assert.Equal("wo-bf-45", byFrance!.RecordId);
        Assert.Equal("wo-cl-45", coombeLane!.RecordId);
        Assert.Equal("JBB-2026-001-WO-0045", byFrance.TagReference);
        Assert.Equal("WO-0045", byFrance.Reference);

        Assert.Null(await provider.FindByTagAsync("WO-0045", CancellationToken.None));
        Assert.Equal("wo-bf-48", (await provider.FindByTagAsync("WO-0048", CancellationToken.None))!.RecordId);
        Assert.Equal("JBB-2026-001-WO-0048", (await provider.FindAsync("wo-bf-48", CancellationToken.None))!.TagReference);
        Assert.All(await provider.ForProjectAsync(ByFrance, CancellationToken.None),
            record => Assert.StartsWith("JBB-2026-001-WO-", record.TagReference));
    }

    [Fact]
    public void Planner_movesUniqueTagsWhole_followsTheAuditTrailOnCollisions_andLeavesTheRest()
    {
        var orders = new[]
        {
            new RetagOrder("wo-bf-45", ByFrance, "JBB-2026-001", 45, "WO-0045"),
            new RetagOrder("wo-cl-45", CoombeLane, "JBB-2026-005", 45, "WO-0045"),
            new RetagOrder("wo-bf-48", ByFrance, "JBB-2026-001", 48, "WO-0048")
        };
        var links = new[]
        {
            new RetagLink("thread-farrants", "wo-bf-45"),
            new RetagLink("thread-farrants", "wo-bf-48"),
            new RetagLink("thread-both", "wo-bf-45"),
            new RetagLink("thread-both", "wo-cl-45")
        };

        var plan = WorkOrderRetagPlanner.Plan(orders, links);

        var whole = Assert.Single(plan.WholeTagMoves);
        Assert.Equal(("JPMS/WO-0048", "JPMS/JBB-2026-001-WO-0048"), (whole.LegacyTag, whole.QualifiedTag));
        var byThread = Assert.Single(plan.ConversationMoves);
        Assert.Equal(("JPMS/WO-0045", "thread-farrants", "JPMS/JBB-2026-001-WO-0045"), (byThread.LegacyTag, byThread.ConversationId, byThread.QualifiedTag));
        var collision = Assert.Single(plan.Collisions);
        Assert.Equal("JPMS/WO-0045", collision.LegacyTag);
        Assert.Equal(new[] { "JPMS/JBB-2026-001-WO-0045", "JPMS/JBB-2026-005-WO-0045" }, collision.CandidateTags);
    }

    private static async Task<JpmsContext> ContextWithTheCollisionAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"work-order-tags-{Guid.NewGuid():N}").Options);
        context.Projects.Add(new ProjectEntity { ProjectId = ByFrance, Reference = "JBB-2026-001", Name = "By France", ClientName = "Client" });
        context.Projects.Add(new ProjectEntity { ProjectId = CoombeLane, Reference = "JBB-2026-005", Name = "Coombe Lane", ClientName = "Client" });
        context.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "sub-farrant", CompanyName = "Farrant Flooring" });
        context.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "sub-hamilton", CompanyName = "Hamilton Glass" });
        WorkOrderBillFixture.AddOrder(context, "wo-bf-45", ByFrance, 45, "sub-farrant", 4634m, ("FLR-LVT", 4634m));
        WorkOrderBillFixture.AddOrder(context, "wo-cl-45", CoombeLane, 45, "sub-hamilton", 5045.23m, ("WDR-SPG", 5045.23m));
        WorkOrderBillFixture.AddOrder(context, "wo-bf-48", ByFrance, 48, "sub-farrant", 42042m, ("FLR-LVT", 42042m));
        await context.SaveChangesAsync();
        return context;
    }
}
