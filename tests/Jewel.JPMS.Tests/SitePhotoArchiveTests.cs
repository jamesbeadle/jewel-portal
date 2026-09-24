using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Progress.SitePhotos;
using Jewel.JPMS.Contracts.Progress;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The pool's archive (2026-09-24, Nigel, for Jeremy's weekly report): the photographs the run
// judges not to be progress are set aside with their reason and the week they were dumped for,
// and KEPT — out of the waiting view, never filed while archived, back with one restore.
public sealed class SitePhotoArchiveTests
{
    private const string Jeremy = "jeremy@jewelbb.co.uk";
    private static readonly DateOnly WeekEnding = new(2026, 9, 24);

    [Fact]
    public async Task Archiving_keepsThePhoto_withItsReason_andTakesItOutOfTheWaitingView()
    {
        await using var context = NewContext();
        await Pool(context, "screenshot", "site-view");

        var result = await Archive(context, "screenshot", "missing");

        Assert.Equal(1, result.ArchivedCount);
        Assert.Equal(SitePhotoArchiving.NotFound, result.Outcomes[1].Result);
        var archived = (await List(context, new ListSitePhotos(ArchivedOnly: true))).Single();
        Assert.Equal("screenshot", archived.SitePhotoId);
        Assert.Equal(SitePhotoArchiveReason.Screenshot, archived.Archive!.Reason);
        Assert.Equal("proj-1", archived.Archive.ProjectId);
        Assert.Equal(WeekEnding, archived.Archive.PeriodEnd);
        Assert.Equal(Jeremy, archived.Archive.ArchivedByEmail);
        Assert.Equal(new[] { "site-view" }, (await List(context, new ListSitePhotos(UnfiledOnly: true))).Select(photo => photo.SitePhotoId));
        Assert.Equal(2, await context.SitePhotos.CountAsync());
    }

    [Fact]
    public async Task AFiledPhoto_isRefused_andAnArchivedOne_keepsItsFirstReason()
    {
        await using var context = NewContext();
        await Pool(context, "filed", "once");
        (await context.SitePhotos.SingleAsync(row => row.SitePhotoId == "filed")).FiledToProgressUpdateId = "monday";
        await context.SaveChangesAsync();

        var first = await Archive(context, "filed", "once");
        var again = await Archive(context, "once");

        Assert.Equal(SitePhotoArchiving.AlreadyFiled, first.Outcomes[0].Result);
        Assert.Equal(SitePhotoArchiving.Archived, first.Outcomes[1].Result);
        Assert.Equal(SitePhotoArchiving.AlreadyArchived, again.Outcomes[0].Result);
    }

    [Fact]
    public async Task AnArchivedPhoto_isNotFiled_untilItIsRestored()
    {
        await using var context = NewContext();
        await Pool(context, "drawing");
        await Archive(context, "drawing");

        var entity = await context.SitePhotos.SingleAsync();
        Assert.Equal(SitePhotoFiling.Archived, SitePhotoFilingRefusals.For(entity, "monday")!.Result);

        await new RestoreSitePhotoHandler(context).HandleAsync(new RestoreSitePhoto("drawing"), CancellationToken.None);

        Assert.Null(SitePhotoFilingRefusals.For(entity, "monday"));
        Assert.True((await List(context, new ListSitePhotos(UnfiledOnly: true))).Single().IsWaiting);
    }

    private static Task<SitePhotoArchivingResult> Archive(JpmsContext context, params string[] ids) =>
        new ArchiveSitePhotosHandler(context).HandleAsync(
            new ArchiveSitePhotos(
                ids.Select(id => new SitePhotoToArchive(id, SitePhotoArchiveReason.Screenshot, "Screenshot of the kitchen drawing")).ToList(),
                "proj-1", WeekEnding, Jeremy),
            CancellationToken.None);

    private static Task<IReadOnlyList<SitePhoto>> List(JpmsContext context, ListSitePhotos query) =>
        new ListSitePhotosHandler(context).HandleAsync(query, CancellationToken.None);

    private static async Task Pool(JpmsContext context, params string[] ids)
    {
        foreach (var id in ids)
        {
            context.SitePhotos.Add(new SitePhotoEntity
            {
                SitePhotoId = id, FileName = $"{id}.jpg", BlobRef = $"site-photos/pool/{id}/{id}.jpg",
                ContentType = "image/jpeg", ContentHash = id, UploadedByEmail = Jeremy, UploadedAt = DateTimeOffset.UtcNow
            });
        }
        await context.SaveChangesAsync();
    }

    private static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"site-photo-archive-{Guid.NewGuid():N}").Options);
}
