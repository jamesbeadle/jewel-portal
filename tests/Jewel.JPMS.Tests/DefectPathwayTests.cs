using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks.Providers;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// 2026-09-16: the same DEF-#### is a trade's workmanship (Subcontractor pane) or a merchant's
// faulty goods (Supplier pane, since 2026-09-07), so a defect's mail no longer files by its TYPE.
// The pane that stages the tag decides; failing that the defect's own company (the work order's
// road); failing that Subcontractor, as every defect filed before.
public sealed class DefectPathwayTests
{
    private const string Project = "P-BF";

    [Fact]
    public void TheType_isPathwayNeutral() =>
        Assert.Null(TriageCategories.BucketFor(RecordType.Defect));

    [Fact]
    public void ANeutralRecord_takesTheChoiceFirst_thenItsOwnPathway()
    {
        var merchantsDefect = new LinkableRecord(RecordType.Defect, "def-1", Project, "DEF-0001", "DEF-0001", "Cracked tiles", Pathway: "Supplier");
        var unassignedDefect = merchantsDefect with { Pathway = "Subcontractor" };

        // The pane's choice wins — a merchant's defect tagged from the Subcontractor pane files there.
        Assert.Equal(TriageCategories.Subcontractor, TriageCategories.BucketFor(merchantsDefect, TriageCategories.Subcontractor));
        Assert.Equal(TriageCategories.Supplier, TriageCategories.BucketFor(unassignedDefect, TriageCategories.Supplier));
        // Nobody chose (the defect page's Find & tag, its composer): the defect's company answers.
        Assert.Equal(TriageCategories.Supplier, TriageCategories.BucketFor(merchantsDefect, null));
        Assert.Equal(TriageCategories.Subcontractor, TriageCategories.BucketFor(unassignedDefect, null));
        // A cost centre keeps its 2026-08 shape: the triager's choice, nothing else.
        var costCentre = new LinkableRecord(RecordType.CostCentre, "cc-1", Project, "FLR-LVT", "FLR-LVT", "Flooring");
        Assert.Equal(TriageCategories.Client, TriageCategories.BucketFor(costCentre, TriageCategories.Client));
        Assert.Null(TriageCategories.BucketFor(costCentre, null));
    }

    [Fact]
    public void ATypedRecord_answersForItself_andIgnoresTheChoice()
    {
        var request = new LinkableRecord(RecordType.Request, "req-1", Project, "RFI-001", "JBB-2026-001-RFI-001", "Lintel");
        Assert.Equal(TriageCategories.Client, TriageCategories.BucketFor(request, TriageCategories.Subcontractor));
        var supplierOrder = new LinkableRecord(RecordType.WorkOrder, "wo-1", Project, "WO-0001", "JBB-2026-001-WO-0001", "Bricks", Pathway: "Supplier");
        Assert.Equal(TriageCategories.Supplier, TriageCategories.BucketFor(supplierOrder, TriageCategories.Subcontractor));
        var todo = new LinkableRecord(RecordType.Todo, "t-1", "", "TODO-0001", "TODO-0001", "Ring the tiler");
        Assert.Null(TriageCategories.BucketFor(todo, null));
    }

    [Fact]
    public async Task Provider_carriesTheCompanysPathwayOnEveryDefect()
    {
        await using var context = await ContextWithThreeDefectsAsync();
        var provider = new DefectLinkProvider(context);

        Assert.Equal("Supplier", (await provider.FindAsync("def-merchant", CancellationToken.None))!.Pathway);
        Assert.Equal("Subcontractor", (await provider.FindAsync("def-trade", CancellationToken.None))!.Pathway);
        Assert.Equal("Subcontractor", (await provider.FindAsync("def-unassigned", CancellationToken.None))!.Pathway);
        Assert.Equal("Supplier", (await provider.FindByTagAsync("DEF-0002", CancellationToken.None))!.Pathway);
        Assert.Equal(
            new[] { "Subcontractor", "Supplier", "Subcontractor" },
            (await provider.ForProjectAsync(Project, CancellationToken.None)).Select(record => record.Pathway).ToArray());
    }

    private static async Task<JpmsContext> ContextWithThreeDefectsAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"defect-pathways-{Guid.NewGuid():N}").Options);
        context.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "sub-farrant", CompanyName = "Farrant Flooring", Category = (int)DirectoryCategory.Subcontractor });
        context.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "sup-travis", CompanyName = "Travis Perkins", Category = (int)DirectoryCategory.Supplier });
        context.Defects.Add(new DefectEntity { DefectId = "def-unassigned", ProjectId = Project, Number = 1, Description = "Scuffed skirting", RaisedAt = DateTimeOffset.UtcNow });
        context.Defects.Add(new DefectEntity { DefectId = "def-merchant", ProjectId = Project, Number = 2, Description = "Cracked tiles delivered", SubcontractorId = "sup-travis", RaisedAt = DateTimeOffset.UtcNow });
        context.Defects.Add(new DefectEntity { DefectId = "def-trade", ProjectId = Project, Number = 3, Description = "Lifting LVT", SubcontractorId = "sub-farrant", RaisedAt = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync();
        return context;
    }
}
