using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Manual.Commands;
using Jewel.JPMS.Api.Features.Manual.Queries;
using Jewel.JPMS.Contracts.Manual;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

public sealed partial class SiteManualTests
{
    private static Task<ManualModule> ReviseAsync(JpmsContext context, string id) =>
        new ReviseManualModuleHandler(context, Loader(context)).HandleAsync(new ReviseManualModule(id, Office), default);

    private static Task<ManualModule> EditAsync(JpmsContext context, ManualModule module, string body) =>
        new UpdateManualModuleDraftHandler(context, Loader(context)).HandleAsync(
            new UpdateManualModuleDraft(module.ManualModuleId, module.Title, module.Purpose, body, module.OwnerEmail, module.ApproverEmail,
                module.Audience, module.LinkedFormSlugs, module.LinkedStandards, "Second thoughts", null, Office), default);

    [Fact]
    public async Task ARevision_keepsTheApprovedTextInFrontOfTheSite_untilItIsApprovedInItsTurn()
    {
        await using var context = NewContext();
        var module = await PublishAsync(context, "RES-01");
        await AcknowledgeAsync(context, module.ManualModuleId);

        var revising = await ReviseAsync(context, module.ManualModuleId);
        await EditAsync(context, revising, "Second text");
        var duringRevision = await ViewAsync(context, ManualView.SiteManager);
        await SubmitAsync(context, module.ManualModuleId);
        await ApproveAsync(context, module.ManualModuleId);
        var afterRevision = await ViewAsync(context, ManualView.SiteManager);
        var detail = await new GetManualModuleHandler(context).HandleAsync(new GetManualModule(module.ManualModuleId), default);

        Assert.Equal(2, revising.Version);
        Assert.True(revising.IsBeingRevised);
        Assert.Equal("First text", Assert.Single(duringRevision.Modules).Body);
        Assert.True(Assert.Single(duringRevision.Modules).HasAcknowledged);
        var republished = Assert.Single(afterRevision.Modules);
        Assert.Equal("Second text", republished.Body);
        Assert.Equal(2, republished.Version);
        Assert.False(republished.HasAcknowledged);
        Assert.Equal(new[] { 2, 1 }, detail!.Versions.Select(version => version.Version));
        Assert.True(detail.Versions[0].IsCurrent);
        Assert.NotNull(detail.Versions[1].SupersededAt);
        Assert.Equal("Second thoughts", detail.Versions[0].ChangeSummary);
        Assert.Equal(1, detail.Versions[1].AcknowledgedCount);
    }

    [Fact]
    public async Task AnAcknowledgement_isRecordedOncePerVersion_andRefusedWhereNothingIsPublished()
    {
        await using var context = NewContext();
        var module = await PublishAsync(context, "STD-01");
        var draft = await CreateAsync(context, "REF-01");

        var first = await AcknowledgeAsync(context, module.ManualModuleId);
        var again = await AcknowledgeAsync(context, module.ManualModuleId);
        var rows = await context.ManualAcknowledgements.CountAsync();

        Assert.Equal(first.ManualAcknowledgementId, again.ManualAcknowledgementId);
        Assert.Equal(1, rows);
        Assert.Equal("Sam Smith", first.TypedName);
        await Assert.ThrowsAsync<InvalidOperationException>(() => AcknowledgeAsync(context, draft.ManualModuleId));
    }

    [Fact]
    public async Task AView_showsOnlyTheModulesPublishedToIt_andNeverARetiredOne()
    {
        await using var context = NewContext();
        await PublishAsync(context, "GOV-01");
        var foremenExcluded = await PublishAsync(context, "RES-05", new ManualAudience(true, false, false));
        var retired = await PublishAsync(context, "REP-01");
        await new RetireManualModuleHandler(context, Loader(context)).HandleAsync(new RetireManualModule(retired.ManualModuleId, Katy), default);

        var foreman = await ViewAsync(context, ManualView.Foreman);
        var siteManager = await ViewAsync(context, ManualView.SiteManager);
        var office = await ViewAsync(context, ManualView.Office);

        Assert.Equal(new[] { "GOV-01" }, foreman.Modules.Select(module => module.Code));
        Assert.Equal(new[] { "GOV-01", "RES-05" }, siteManager.Modules.Select(module => module.Code));
        Assert.Equal(new[] { "GOV-01", "RES-05" }, office.Modules.Select(module => module.Code));
        Assert.Equal(ManualModuleStatus.Superseded, (ManualModuleStatus)(await context.ManualModules.SingleAsync(row => row.ManualModuleId == retired.ManualModuleId)).Status);
        Assert.Contains(foremenExcluded.Code, siteManager.Modules.Select(module => module.Code));
    }

    [Fact]
    public async Task AReturnToDraft_carriesTheReasonToTheOwner_andTheGateRejectsWhatItsColumnsCannotHold()
    {
        await using var context = NewContext();
        var module = await CreateAsync(context, "WFL-01");
        await SubmitAsync(context, module.ManualModuleId);

        var returned = await new ReturnManualModuleToDraftHandler(context, Loader(context))
            .HandleAsync(new ReturnManualModuleToDraft(module.ManualModuleId, "Name the cc address", Katy), default);
        var validation = new ManualValidationGate();
        var badCode = validation.Check(new CreateManualModule("daily", "Daily", "", "text", "", "", ManualAudience.Everyone, Array.Empty<string>(), "", "", null));
        var badForm = validation.Check(new CreateManualModule("RUN-09", "Daily", "", "text", "", "", ManualAudience.Everyone, new[] { "no-such-form" }, "", "", null));

        Assert.Equal(ManualModuleStatus.Draft, returned.Status);
        Assert.Contains("Returned to draft: Name the cc address", returned.ChangeSummary);
        Assert.True(badCode.HasFailed);
        Assert.True(badForm.HasFailed);
    }
}
