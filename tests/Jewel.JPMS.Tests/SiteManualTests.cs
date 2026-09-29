using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Manual;
using Jewel.JPMS.Api.Features.Manual.Commands;
using Jewel.JPMS.Api.Features.Manual.Queries;
using Jewel.JPMS.Contracts.Manual;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The site manual as controlled modules (2026-09-29, Nigel's Site Manuals and Operating Systems
// request): the JBB baseline loads as drafts, a draft never reaches a site view, an approval publishes
// one version and supersedes the last, and every reader acknowledges the version they read.
public sealed partial class SiteManualTests
{
    private const string Office = "office@jewelbb.co.uk";
    private const string Katy = "katy-louise.hicks@jewelbb.co.uk";
    private const string SiteManager = "sam@site.co.uk";
    private const int BaselineModuleCount = 21;

    private static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"site-manual-{Guid.NewGuid():N}").Options);

    private static ManualModuleLoader Loader(JpmsContext context) => new(context);

    private static Task<ManualBaselineImport> ImportAsync(JpmsContext context) =>
        new ImportManualBaselineHandler(context, Loader(context)).HandleAsync(new ImportManualBaseline(Office), default);

    private static Task<ManualModule> CreateAsync(JpmsContext context, string code, ManualAudience? audience = null) =>
        new CreateManualModuleHandler(context, Loader(context)).HandleAsync(
            new CreateManualModule(code, $"Module {code}", "Its purpose", "First text", Office, Katy,
                audience ?? ManualAudience.Everyone, Array.Empty<string>(), "", "", null, Office), default);

    private static Task<ManualModule> SubmitAsync(JpmsContext context, string id) =>
        new SubmitManualModuleForReviewHandler(context, Loader(context)).HandleAsync(new SubmitManualModuleForReview(id, Office), default);

    private static Task<ManualModule> ApproveAsync(JpmsContext context, string id) =>
        new ApproveManualModuleHandler(context, Loader(context)).HandleAsync(new ApproveManualModule(id, null, Katy), default);

    private static async Task<ManualModule> PublishAsync(JpmsContext context, string code, ManualAudience? audience = null)
    {
        var created = await CreateAsync(context, code, audience);
        await SubmitAsync(context, created.ManualModuleId);
        return await ApproveAsync(context, created.ManualModuleId);
    }

    private static Task<ManualViewReading> ViewAsync(JpmsContext context, ManualView view, string reader = SiteManager) =>
        new GetManualViewHandler(context).HandleAsync(new GetManualView(view), reader, default);

    private static Task<ManualAcknowledgement> AcknowledgeAsync(JpmsContext context, string id, string reader = SiteManager) =>
        new AcknowledgeManualModuleHandler(context, Loader(context)).HandleAsync(new AcknowledgeManualModule(id, "Sam Smith", reader), default);

    [Fact]
    public async Task TheBaseline_loadsEveryModuleAsADraft_andNeverTwice()
    {
        await using var context = NewContext();

        var first = await ImportAsync(context);
        var second = await ImportAsync(context);
        var master = await new ListManualModulesHandler(context).HandleAsync(new ListManualModules(), default);

        Assert.Equal(BaselineModuleCount, first.CreatedCount);
        Assert.Equal(0, second.CreatedCount);
        Assert.Equal(BaselineModuleCount, second.SkippedCodes.Count);
        Assert.All(master, module => Assert.Equal(ManualModuleStatus.Draft, module.Status));
        Assert.All(master, module => Assert.False(string.IsNullOrWhiteSpace(module.Body)));
        Assert.Equal("GOV-01", master[0].Code);
        Assert.Equal("REF-02", master[^1].Code);
    }

    [Fact]
    public async Task ADraft_neverReachesASiteView_untilItIsApproved()
    {
        await using var context = NewContext();
        var draft = await CreateAsync(context, "RUN-01");

        var before = await ViewAsync(context, ManualView.SiteManager);
        await SubmitAsync(context, draft.ManualModuleId);
        var inReview = await ViewAsync(context, ManualView.SiteManager);
        await ApproveAsync(context, draft.ManualModuleId);
        var after = await ViewAsync(context, ManualView.SiteManager);

        Assert.Empty(before.Modules);
        Assert.Empty(inReview.Modules);
        var published = Assert.Single(after.Modules);
        Assert.Equal("First text", published.Body);
        Assert.Equal(1, published.Version);
        Assert.False(published.HasAcknowledged);
    }

    [Fact]
    public async Task ADraft_cannotBeSentForReview_withoutAnOwnerAndAnApprover()
    {
        await using var context = NewContext();
        await ImportAsync(context);
        var loaded = await context.ManualModules.FirstAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => SubmitAsync(context, loaded.ManualModuleId));
    }
}
