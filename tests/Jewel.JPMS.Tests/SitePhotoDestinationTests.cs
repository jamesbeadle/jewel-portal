using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Progress.SitePhotos;
using Jewel.JPMS.Contracts.Progress;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// Where a filed pool photo went (2026-09-24, the FD: Site Photos sat under By France and read as
// By France's photos when it was every site's): every filed photo names its project and the day
// its update records, and the pool narrows to one project's photos — filed or archived.
public sealed class SitePhotoDestinationTests
{
    private static readonly DateTimeOffset Monday = new(2026, 9, 21, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AFiledPhoto_namesItsProject_andTheDayOfItsUpdate()
    {
        await using var context = await SeededContext();

        var photos = await List(context, new ListSitePhotos());

        var filed = photos.Single(photo => photo.SitePhotoId == "by-france-monday");
        Assert.Equal("JBB-2026-001", filed.FiledTo!.ProjectReference);
        Assert.Equal("By France", filed.FiledTo.ProjectName);
        Assert.Equal(Monday, filed.FiledTo.WorkDate);
        Assert.Null(photos.Single(photo => photo.SitePhotoId == "unfiled").FiledTo);
    }

    [Fact]
    public async Task OneProjectsPhotos_areThoseFiledToIt_orArchivedFromItsWeeks()
    {
        await using var context = await SeededContext();

        var photos = await List(context, new ListSitePhotos(ProjectId: "by-france"));

        Assert.Equal(new[] { "by-france-archived", "by-france-monday" },
            photos.Select(photo => photo.SitePhotoId).OrderBy(id => id));
        Assert.All(photos, photo => Assert.True(photo.BelongsTo("by-france")));
    }

    private static Task<IReadOnlyList<SitePhoto>> List(JpmsContext context, ListSitePhotos query) =>
        new ListSitePhotosHandler(context).HandleAsync(query, CancellationToken.None);

    private static async Task<JpmsContext> SeededContext()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"site-photo-destinations-{Guid.NewGuid():N}").Options);
        context.Projects.Add(new ProjectEntity { ProjectId = "by-france", Reference = "JBB-2026-001", Name = "By France" });
        context.Projects.Add(new ProjectEntity { ProjectId = "coombe-lane", Reference = "JBB-2026-005", Name = "Coombe Lane" });
        context.ProgressUpdates.Add(new ProgressUpdateEntity { ProgressUpdateId = "monday", ProjectId = "by-france", Title = "Monday", WorkDate = Monday });
        context.ProgressUpdates.Add(new ProgressUpdateEntity { ProgressUpdateId = "coombe-day", ProjectId = "coombe-lane", Title = "Tuesday" });
        context.SitePhotos.AddRange(
            Photo("by-france-monday", filedTo: ("by-france", "monday")),
            Photo("coombe-lane-tuesday", filedTo: ("coombe-lane", "coombe-day")),
            Photo("by-france-archived", archivedFor: "by-france"),
            Photo("unfiled"));
        await context.SaveChangesAsync();
        return context;
    }

    private static SitePhotoEntity Photo(string id, (string Project, string Update)? filedTo = null, string? archivedFor = null) => new()
    {
        SitePhotoId = id, FileName = $"{id}.jpg", ContentType = "image/jpeg", ContentHash = id,
        UploadedAt = Monday,
        FiledToProjectId = filedTo?.Project, FiledToProgressUpdateId = filedTo?.Update,
        FiledAt = filedTo is null ? null : Monday,
        ArchivedForProjectId = archivedFor, ArchivedAt = archivedFor is null ? null : Monday,
        ArchiveReason = archivedFor is null ? null : SitePhotoArchiveReason.NotProgress
    };
}
