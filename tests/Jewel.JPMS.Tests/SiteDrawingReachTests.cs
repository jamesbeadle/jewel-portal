using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.SiteAccess.Site;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Jewel.JPMS.Tests.SiteDrawingLinkFixture;

namespace Jewel.JPMS.Tests;

/// <summary>What a scan reaches: the link's folders and nothing else, the revision the register
/// would issue, and a view count that inline taps never inflate.</summary>
public sealed class SiteDrawingReachTests
{
    [Fact]
    public async Task ARevisionOutsideTheLinksFolders_isNothingToTheLink()
    {
        await using var context = NewContext();
        await SeedRegister(context);
        var link = await Link(context, "within", Structural, includeSubFolders: false);

        var reach = new SiteDrawingReach(context);
        Assert.NotNull(await reach.RevisionWithinAsync(link.Entity, StructuralApproved, CancellationToken.None));
        Assert.Null(await reach.RevisionWithinAsync(link.Entity, ArchitecturalApproved, CancellationToken.None));
        Assert.Null(await reach.RevisionWithinAsync(link.Entity, SteelOnly, CancellationToken.None));
        Assert.Null(await reach.RevisionWithinAsync(link.Entity, OtherProjects, CancellationToken.None));
        Assert.Null(await reach.RevisionWithinAsync(link.Entity, "no-such-revision", CancellationToken.None));
    }

    [Fact]
    public async Task SubFolders_areReached_onlyWhenTheLinkIncludesThem()
    {
        await using var context = NewContext();
        await SeedRegister(context);
        var deep = await Link(context, "deep", Structural, includeSubFolders: true);
        var shallow = await Link(context, "shallow", Structural, includeSubFolders: false);

        var listing = new SiteDrawingListing(context, new SiteDrawingReach(context));
        var deepRows = (await listing.ReadAsync(deep.Entity, CancellationToken.None)).Rows;
        var shallowRows = (await listing.ReadAsync(shallow.Entity, CancellationToken.None)).Rows;

        var steel = Assert.Single(deepRows, row => row.DrawingRevisionId == SteelOnly);
        Assert.Equal("Structural / Steel", steel.FolderPath);
        Assert.DoesNotContain(shallowRows, row => row.DrawingRevisionId == SteelOnly);
        Assert.DoesNotContain(deepRows, row => row.DrawingRevisionId == ArchitecturalApproved);
    }

    [Fact]
    public async Task TheApprovedRevisionIsShown_elseTheNewest_markedNotApproved()
    {
        await using var context = NewContext();
        await SeedRegister(context);
        var link = await Link(context, "shown", Structural, includeSubFolders: false);

        var page = await new SiteDrawingListing(context, new SiteDrawingReach(context)).ReadAsync(link.Entity, CancellationToken.None);

        var approvedDrawing = Assert.Single(page.Rows, row => row.Label.StartsWith("S-100"));
        Assert.Equal(StructuralApproved, approvedDrawing.DrawingRevisionId);
        Assert.True(approvedDrawing.IsApproved);
        var unapprovedDrawing = Assert.Single(page.Rows, row => row.Label.StartsWith("S-200"));
        Assert.Equal(StructuralNewestUnapproved, unapprovedDrawing.DrawingRevisionId);
        Assert.False(unapprovedDrawing.IsApproved);
        Assert.Equal("By France", page.ProjectName);
    }

    [Fact]
    public async Task AScanIsCounted_andAnInlineViewOfAFile_doesNotCountAsAView()
    {
        await using var context = NewContext();
        await SeedRegister(context);
        var link = await Link(context, "scan", Structural, includeSubFolders: false);

        await new SiteLinkScans(context).RecordAsync(link.Entity, CancellationToken.None);
        Assert.Equal(1, (await context.SiteDrawingLinks.AsNoTracking().SingleAsync()).ScanCount);

        var reach = new SiteDrawingReach(context);
        var endpoints = new SitePublicEndpoints(
            new SiteLinkResolver(context, NullLogger<SiteLinkResolver>.Instance),
            new SiteDrawingListing(context, reach), new SiteLinkScans(context), reach, new StubBlobStore(), context);
        Assert.IsType<FileStreamResult>(await endpoints.OpenFile(RequestFor("?inline=1"), link.Token, StructuralApproved));
        Assert.Equal(0, await ViewCountOf(context, StructuralApproved));
        Assert.IsType<FileStreamResult>(await endpoints.OpenFile(RequestFor(""), link.Token, StructuralApproved));
        Assert.Equal(1, await ViewCountOf(context, StructuralApproved));
        Assert.IsType<NotFoundResult>(await endpoints.OpenFile(RequestFor(""), link.Token, ArchitecturalApproved));
        Assert.IsType<NotFoundResult>(await endpoints.OpenFile(RequestFor(""), "garbage", StructuralApproved));
        Assert.IsType<ContentResult>(await endpoints.OpenPage(RequestFor(""), link.Token));
        Assert.Equal(2, (await context.SiteDrawingLinks.AsNoTracking().SingleAsync()).ScanCount);
    }

    private static async Task<int> ViewCountOf(JpmsContext context, string revisionId) =>
        (await context.DrawingRevisions.AsNoTracking().SingleAsync(row => row.DrawingRevisionId == revisionId)).ViewCount;
}
