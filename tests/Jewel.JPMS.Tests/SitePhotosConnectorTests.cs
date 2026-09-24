using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Features.Ai.Tools.Actions;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The site photo pool (2026-09-16, James: "a big dumping ground for photos and any project …
// jeremy can do his normal weekly report mcp stuff and it does the matching"). A tool call carries
// words, so the connector never takes an image: it reads the pool, matches the laptop's SHA-256
// fingerprints against it and files the matches onto the day's update. Reads for every internal
// role, writes behind the Progress page's contributor gate, none of it for a subcontractor.
public sealed class SitePhotosConnectorTests
{
    private static SignedInUser UserWith(params Role[] roles) => new("test@jewelbb.co.uk", "Test User", roles);

    [Fact]
    public void TheReads_reachEveryInternalRole_andNoSubcontractor()
    {
        var qs = AiToolCatalogue.ForConnector(UserWith(Role.QuantitySurveyor)).Select(tool => tool.Name).ToList();
        Assert.Contains(AiSitePhotoTools.ListSitePhotos, qs);
        Assert.Contains(AiSitePhotoTools.MatchSitePhotos, qs);

        var subcontractor = AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(tool => tool.Name).ToList();
        Assert.DoesNotContain(AiSitePhotoTools.ListSitePhotos, subcontractor);
        Assert.DoesNotContain(AiSitePhotoTools.MatchSitePhotos, subcontractor);
    }

    [Fact]
    public void MatchSitePhotos_saysHowToHash_andWhereFilingHappens()
    {
        var match = AiToolCatalogue.Find(AiSitePhotoTools.MatchSitePhotos)!;
        Assert.Equal(AiToolKind.Read, match.Kind);
        Assert.Contains("SHA-256", match.Description);
        Assert.Contains("shasum -a 256", match.Description);
        Assert.Contains("file_site_photos", match.Description);

        var addPhotos = AiToolCatalogue.Find(AiProgressPhotoTools.AddProgressPhotos)!;
        Assert.Contains("match_site_photos", addPhotos.Description);
    }

    [Fact]
    public void FileSitePhotos_isTheContributorsWrite_stampedWithWhoFiled()
    {
        var file = AiActionRegistry.Find("file_site_photos");
        Assert.NotNull(file);
        Assert.Equal(typeof(FileSitePhotos), file!.CommandType);
        Assert.Contains("FiledByEmail", file.EmailStamps);
        Assert.Equal(AiActionRegistry.Find("create_progress_update")!.VisibleTo, file.VisibleTo);
        Assert.True(file.VisibleTo.Includes(Role.SiteManager));
        Assert.False(file.VisibleTo.Includes(Role.Subcontractor));
        Assert.Contains("match_site_photos", file.Notes);
        Assert.False(file.RequiresConfirmation);
    }

    [Fact]
    public void DeleteSitePhoto_confirmsFirst()
    {
        var delete = AiActionRegistry.Find("delete_site_photo");
        Assert.NotNull(delete);
        Assert.True(delete!.RequiresConfirmation);
        Assert.Equal(typeof(DeleteSitePhoto), delete.CommandType);
    }

    [Fact]
    public void ArchiveSitePhotos_isTheContributorsWrite_readingTheReportSkill_andCanBeRestored()
    {
        var archive = AiActionRegistry.Find("archive_site_photos");
        Assert.NotNull(archive);
        Assert.Equal(typeof(ArchiveSitePhotos), archive!.CommandType);
        Assert.Contains("ArchivedByEmail", archive.EmailStamps);
        Assert.Equal(AiActionRegistry.Find("file_site_photos")!.VisibleTo, archive.VisibleTo);
        Assert.Contains("jpms-contractors-report", archive.Notes);
        Assert.Contains("archive_site_photos", AiToolCatalogue.Find(AiSitePhotoTools.MatchSitePhotos)!.Description);

        var restore = AiActionRegistry.Find("restore_site_photo");
        Assert.NotNull(restore);
        Assert.Equal(typeof(RestoreSitePhoto), restore!.CommandType);
        Assert.False(restore.VisibleTo.Includes(Role.Subcontractor));
    }
}
