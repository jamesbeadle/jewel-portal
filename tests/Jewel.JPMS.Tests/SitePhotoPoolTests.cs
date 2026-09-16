using ImageMagick;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Progress.Photos;
using Jewel.JPMS.Api.Features.Progress.SitePhotos;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Contracts.Progress;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The site photo pool (2026-09-16): keyed by the SHA-256 of the file as dropped, so the same
// bytes are one row however many times or under whatever name they arrive; matched by the same
// hash a laptop computes for the same file; filed onto one update only, as an ordinary progress
// photo carrying the pool's hash, the pool row stamped with where it went; a refused photo never
// stops the batch.
public sealed class SitePhotoPoolTests
{
    private const string Uploader = "jeremy@jewelbb.co.uk";

    [Fact]
    public async Task TheSameFile_droppedTwice_isOnePoolRow_foundByItsLaptopHash()
    {
        await using var context = NewContext();
        var store = new MemoryPhotoStore();
        var picture = Png(MagickColors.Red);

        var first = await new SitePhotoIntake(context, store).TakeAsync(new[] { Incoming("IMG-0001.png", picture) }, Uploader, CancellationToken.None);
        var second = await new SitePhotoIntake(context, store).TakeAsync(
            new[] { Incoming("renamed.png", picture), Incoming("IMG-0002.png", Png(MagickColors.Blue)) }, Uploader, CancellationToken.None);

        Assert.Equal(1, first.StoredCount);
        Assert.Equal(1, second.DuplicateCount);
        Assert.Equal(1, second.StoredCount);
        Assert.Equal(2, await context.SitePhotos.CountAsync());

        var laptopHash = ProgressPhotoContentHash.Of(picture);
        var matches = await new MatchSitePhotosHandler(context).HandleAsync(
            new MatchSitePhotos(new[] { laptopHash.ToUpperInvariant(), "not-in-the-pool" }), CancellationToken.None);
        Assert.Equal(1, matches.FoundCount);
        Assert.Equal(1, matches.MissingCount);
        Assert.Equal("IMG-0001.png", matches.Matches[0].Photo!.FileName);
        Assert.Null(matches.Matches[1].Photo);
    }

    [Fact]
    public async Task Filing_copiesThePhotoOntoTheUpdate_withThePoolsHash_andStampsThePoolRow()
    {
        await using var context = NewContext();
        var store = new MemoryPhotoStore();
        var update = await UpdateOn(context, "proj-1", "day-1");
        var taken = await new SitePhotoIntake(context, store).TakeAsync(
            new[] { Incoming("a.png", Png(MagickColors.Red)), Incoming("b.png", Png(MagickColors.Green)) }, Uploader, CancellationToken.None);
        var ids = taken.Outcomes.Select(outcome => outcome.SitePhotoId!).ToList();

        var result = await new FileSitePhotosHandler(context, store).HandleAsync(
            new FileSitePhotos(update.ProgressUpdateId, ids.Append("no-such-photo").ToList(), Uploader), CancellationToken.None);

        Assert.Equal(2, result.FiledCount);
        Assert.Equal(SitePhotoFiling.NotFound, result.Outcomes[2].Result);
        Assert.Equal(2, result.Update.Photos.Count);
        var filed = await context.ProgressPhotos.Where(row => row.ProgressUpdateId == update.ProgressUpdateId).OrderBy(row => row.SortOrder).ToListAsync();
        var pool = await context.SitePhotos.OrderBy(row => row.FileName).ToListAsync();
        Assert.Equal(pool.Select(row => row.ContentHash), filed.Select(row => row.ContentHash));
        Assert.All(pool, row =>
        {
            Assert.Equal(update.ProgressUpdateId, row.FiledToProgressUpdateId);
            Assert.Equal("proj-1", row.FiledToProjectId);
            Assert.Equal(Uploader, row.FiledByEmail);
            Assert.NotNull(row.FiledAt);
        });
        Assert.All(filed, row => Assert.StartsWith("proj-1/day-1/", row.BlobRef));
        Assert.Equal(4, store.Count);
    }

    [Fact]
    public async Task APhoto_goesOntoOneUpdateOnly_andOneTheUpdateAlreadyHolds_isNotCopiedAgain()
    {
        await using var context = NewContext();
        var store = new MemoryPhotoStore();
        var monday = await UpdateOn(context, "proj-1", "monday");
        var tuesday = await UpdateOn(context, "proj-1", "tuesday");
        var picture = Png(MagickColors.Red);
        var taken = await new SitePhotoIntake(context, store).TakeAsync(new[] { Incoming("a.png", picture) }, Uploader, CancellationToken.None);
        var id = taken.Outcomes[0].SitePhotoId!;

        var first = await new FileSitePhotosHandler(context, store).HandleAsync(new FileSitePhotos(monday.ProgressUpdateId, new[] { id }, Uploader), CancellationToken.None);
        var again = await new FileSitePhotosHandler(context, store).HandleAsync(new FileSitePhotos(monday.ProgressUpdateId, new[] { id }, Uploader), CancellationToken.None);
        var elsewhere = await new FileSitePhotosHandler(context, store).HandleAsync(new FileSitePhotos(tuesday.ProgressUpdateId, new[] { id }, Uploader), CancellationToken.None);

        Assert.Equal(SitePhotoFiling.Filed, first.Outcomes[0].Result);
        Assert.Equal(SitePhotoFiling.AlreadyOnUpdate, again.Outcomes[0].Result);
        Assert.Equal(first.Outcomes[0].ProgressPhotoId, again.Outcomes[0].ProgressPhotoId);
        Assert.Equal(SitePhotoFiling.AlreadyFiledElsewhere, elsewhere.Outcomes[0].Result);
        Assert.Equal(1, await context.ProgressPhotos.CountAsync());

        // The same bytes already on an update by another route (the page's form, say) are recognised
        // by content: the pool photo is marked filed to that update and nothing is copied.
        var second = await new SitePhotoIntake(context, store).TakeAsync(new[] { Incoming("b.png", Png(MagickColors.Blue)) }, Uploader, CancellationToken.None);
        var blueId = second.Outcomes[0].SitePhotoId!;
        var blueHash = (await context.SitePhotos.SingleAsync(row => row.SitePhotoId == blueId)).ContentHash;
        context.ProgressPhotos.Add(new ProgressPhotoEntity
        {
            ProgressPhotoId = "page-photo", ProgressUpdateId = tuesday.ProgressUpdateId, ProjectId = "proj-1",
            FileName = "b-from-the-page.png", BlobRef = "x", ContentType = "image/png", ContentHash = blueHash, UploadedByEmail = Uploader
        });
        await context.SaveChangesAsync();
        var held = await new FileSitePhotosHandler(context, store).HandleAsync(new FileSitePhotos(tuesday.ProgressUpdateId, new[] { blueId }, Uploader), CancellationToken.None);
        Assert.Equal(SitePhotoFiling.AlreadyOnUpdate, held.Outcomes[0].Result);
        Assert.Equal("page-photo", held.Outcomes[0].ProgressPhotoId);
        Assert.Equal(tuesday.ProgressUpdateId, (await context.SitePhotos.SingleAsync(row => row.SitePhotoId == blueId)).FiledToProgressUpdateId);
    }

    [Fact]
    public async Task DeletingAPoolPhoto_leavesItsFiledCopyOnTheUpdate()
    {
        await using var context = NewContext();
        var store = new MemoryPhotoStore();
        var update = await UpdateOn(context, "proj-1", "day-1");
        var taken = await new SitePhotoIntake(context, store).TakeAsync(new[] { Incoming("a.png", Png(MagickColors.Red)) }, Uploader, CancellationToken.None);
        var id = taken.Outcomes[0].SitePhotoId!;
        await new FileSitePhotosHandler(context, store).HandleAsync(new FileSitePhotos(update.ProgressUpdateId, new[] { id }, Uploader), CancellationToken.None);

        await new DeleteSitePhotoHandler(context, store).HandleAsync(new DeleteSitePhoto(id), CancellationToken.None);

        Assert.Equal(0, await context.SitePhotos.CountAsync());
        Assert.Equal(1, await context.ProgressPhotos.CountAsync());
        Assert.Equal(1, store.Count);
        Assert.Empty((await new ListSitePhotosHandler(context).HandleAsync(new ListSitePhotos(UnfiledOnly: true), CancellationToken.None)));
    }

    private static async Task<ProgressUpdateEntity> UpdateOn(JpmsContext context, string projectId, string updateId)
    {
        var update = new ProgressUpdateEntity
        {
            ProgressUpdateId = updateId, ProjectId = projectId, Title = updateId, Description = "notes",
            CreatedByEmail = Uploader, CreatedAt = DateTimeOffset.UtcNow
        };
        context.ProgressUpdates.Add(update);
        await context.SaveChangesAsync();
        return update;
    }

    private static IncomingProgressPhoto Incoming(string name, byte[] bytes) => new(name, "image/png", bytes);

    private static byte[] Png(MagickColor colour)
    {
        using var image = new MagickImage(colour, 4, 4);
        image.Format = MagickFormat.Png;
        return image.ToByteArray();
    }

    private static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"site-photos-{Guid.NewGuid():N}").Options);

    /// <summary>The store as a dictionary: blob ref → bytes, keyed exactly as the Azure store keys.</summary>
    private sealed class MemoryPhotoStore : IProgressPhotoStore
    {
        private readonly Dictionary<string, (byte[] Bytes, string ContentType)> blobs = new();

        public int Count => blobs.Count;

        public async Task<string> UploadAsync(
            string projectId, string progressUpdateId, string photoId,
            string fileName, string contentType, Stream content, CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            var blobRef = $"{projectId}/{progressUpdateId}/{photoId}/{fileName}";
            blobs[blobRef] = (buffer.ToArray(), contentType);
            return blobRef;
        }

        public Task<ProgressPhotoBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
            Task.FromResult(blobs.TryGetValue(blobRef, out var blob)
                ? new ProgressPhotoBlob(new MemoryStream(blob.Bytes), blob.ContentType, blob.Bytes.Length)
                : null);

        public Task DeleteAsync(string blobRef, CancellationToken cancellationToken)
        {
            blobs.Remove(blobRef);
            return Task.CompletedTask;
        }
    }
}
