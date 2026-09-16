using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Procurement;
using Jewel.JPMS.Api.Features.RecordLinks.Providers;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// 2026-09-15: a work order is raised with a supplier as well as a subcontractor (the merchant's
// purchase order is the same record as the trade's), so the pathway its mail files under follows
// the COMPANY the order is placed with — carried per record on LinkableRecord.Pathway — rather
// than the record type, which can only ever say "Subcontractor".
public sealed class WorkOrderPathwayTests
{
    private const string Project = "P-BF";

    [Fact]
    public void Bucket_followsTheCompanyCategory()
    {
        Assert.Equal(TriageCategories.Supplier, CompanyPathways.BucketFor(DirectoryCategory.Supplier));
        Assert.Equal(TriageCategories.Subcontractor, CompanyPathways.BucketFor(DirectoryCategory.Subcontractor));
        // Anything else an old order might point at — and a company missing from the directory —
        // keeps the pre-2026-09-15 answer.
        Assert.Equal(TriageCategories.Subcontractor, CompanyPathways.BucketFor(DirectoryCategory.Client));
        Assert.Equal(TriageCategories.Subcontractor, CompanyPathways.BucketFor(null));
        Assert.Equal("Supplier", CompanyPathways.LabelFor(DirectoryCategory.Supplier));
        Assert.Equal("Subcontractor", CompanyPathways.LabelFor(DirectoryCategory.Other));
    }

    [Fact]
    public void RecordPathway_overridesTheTypeDefault_andOnlyWhenPresent()
    {
        var supplierOrder = new LinkableRecord(RecordType.WorkOrder, "wo-1", Project, "WO-0001", "JBB-2026-001-WO-0001", "Bricks", Pathway: "Supplier");
        var subcontractorOrder = supplierOrder with { Pathway = "Subcontractor" };
        var legacyProjection = supplierOrder with { Pathway = null };

        Assert.Equal(TriageCategories.Supplier, TriageCategories.BucketFor(supplierOrder));
        Assert.Equal(TriageCategories.Subcontractor, TriageCategories.BucketFor(subcontractorOrder));
        Assert.Equal(TriageCategories.BucketFor(RecordType.WorkOrder), TriageCategories.BucketFor(legacyProjection));

        // The type default is unchanged for callers holding only the type.
        Assert.Equal(TriageCategories.Subcontractor, TriageCategories.BucketFor(RecordType.WorkOrder));
        // A pathway-neutral type stays neutral whatever text a projection carries by mistake.
        Assert.Null(TriageCategories.BucketFor(new LinkableRecord(RecordType.Todo, "t-1", "", "TODO-0001", "TODO-0001", "x", Pathway: "nonsense")));
    }

    [Theory]
    [InlineData("Client", "JPMS/Client")]
    [InlineData("subcontractor", "JPMS/Subcontractor")]
    [InlineData(" Supplier ", "JPMS/Supplier")]
    [InlineData("INTERNAL", "JPMS/Internal")]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("Neutral", null)]
    public void PathwayLabel_mapsToItsBucket(string? label, string? expected) =>
        Assert.Equal(expected, TriageCategories.BucketForPathway(label));

    [Fact]
    public async Task Provider_carriesTheCompanysPathwayOnEveryRecord()
    {
        await using var context = await ContextWithBothKindsOfOrderAsync();
        var provider = new WorkOrderLinkProvider(context);

        var merchant = await provider.FindAsync("wo-merchant", CancellationToken.None);
        var trade = await provider.FindAsync("wo-trade", CancellationToken.None);
        var orphan = await provider.FindAsync("wo-orphan", CancellationToken.None);
        Assert.Equal("Supplier", merchant!.Pathway);
        Assert.Equal("Subcontractor", trade!.Pathway);
        Assert.Equal("Subcontractor", orphan!.Pathway);
        Assert.Equal("Travis Perkins", merchant.Summary);

        // The same answer whichever door the record comes through.
        Assert.Equal("Supplier", (await provider.FindByTagAsync("JBB-2026-001-WO-0002", CancellationToken.None))!.Pathway);
        Assert.Equal(
            new[] { "Subcontractor", "Supplier", "Subcontractor" },
            (await provider.ForProjectAsync(Project, CancellationToken.None)).Select(record => record.Pathway).ToArray());
        Assert.Equal(TriageCategories.Supplier, CompanyPathways.BucketFor(await CompanyPathways.CategoryAsync(context, "sup-travis", CancellationToken.None)));
        Assert.Equal(TriageCategories.Subcontractor, CompanyPathways.BucketFor(await CompanyPathways.CategoryAsync(context, "nobody", CancellationToken.None)));
    }

    private static async Task<JpmsContext> ContextWithBothKindsOfOrderAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"work-order-pathways-{Guid.NewGuid():N}").Options);
        context.Projects.Add(new ProjectEntity { ProjectId = Project, Reference = "JBB-2026-001", Name = "By France", ClientName = "Client" });
        context.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "sub-farrant", CompanyName = "Farrant Flooring", Category = (int)DirectoryCategory.Subcontractor });
        context.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "sup-travis", CompanyName = "Travis Perkins", Category = (int)DirectoryCategory.Supplier });
        WorkOrderBillFixture.AddOrder(context, "wo-trade", Project, 3, "sub-farrant", 4634m, ("FLR-LVT", 4634m));
        WorkOrderBillFixture.AddOrder(context, "wo-merchant", Project, 2, "sup-travis", 1200m, ("BLK-MAT", 1200m));
        WorkOrderBillFixture.AddOrder(context, "wo-orphan", Project, 1, "gone-from-directory", 50m, ("MSC", 50m));
        await context.SaveChangesAsync();
        return context;
    }
}
