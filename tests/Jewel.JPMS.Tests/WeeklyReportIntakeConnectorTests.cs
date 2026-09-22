using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Features.Ai.Tools.Actions;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The FD's weekly-report spec, changes 1 and 2 (2026-09-16): the connector can now WRITE the one
// thing the Contractor's Report needed that it could only read — the site note and its
// photographs. create_progress_update is the counterpart to update_progress_update;
// add_progress_photos the counterpart to delete_progress_photo. Both behind the Progress page's
// contributor gate, neither visible to a subcontractor.
public sealed class WeeklyReportIntakeConnectorTests
{
    private static SignedInUser UserWith(params Role[] roles) => new("test@jewelbb.co.uk", "Test User", roles);

    [Fact]
    public void CreateProgressUpdate_reachesTheConnector_asTheCounterpartToUpdate()
    {
        var action = AiActionRegistry.Find("create_progress_update");
        Assert.NotNull(action);
        Assert.Equal(typeof(CreateProgressUpdate), action!.CommandType);
        Assert.Contains("CreatedByEmail", action.EmailStamps);
        Assert.Equal(AiActionRegistry.Find("update_progress_update")!.VisibleTo, action.VisibleTo);
        Assert.Contains("add_progress_photos", action.Description);
    }

    [Fact]
    public void AddProgressPhotos_reachesTheConnector_forContributorsOnly()
    {
        var siteManager = AiToolCatalogue.ForConnector(UserWith(Role.SiteManager)).Select(tool => tool.Name).ToList();
        Assert.Contains(AiProgressPhotoTools.AddProgressPhotos, siteManager);
        Assert.DoesNotContain(AiProgressPhotoTools.AddProgressPhotos,
            AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(tool => tool.Name));

        var tool = AiToolCatalogue.Find(AiProgressPhotoTools.AddProgressPhotos)!;
        Assert.Equal(AiToolKind.Write, tool.Kind);
        Assert.Contains("list_sources", tool.Description);
        Assert.Contains("match_site_photos", tool.Description);
    }
}
