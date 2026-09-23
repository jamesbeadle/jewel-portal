using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Hs;
using Jewel.JPMS.Api.Features.Hs.Audits;
using Jewel.JPMS.Api.Features.Hs.Commands;
using Jewel.JPMS.Api.Features.Hs.Notifications;
using Jewel.JPMS.Api.Features.Hs.Thread.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The thread on a corrective action (2026-09-23, Katy-Louise's asks of 15 Sep): a site manager's
// comment moves the action to In progress and leaves an event; only the officer or a director
// closes one; an Ongoing time-scale has no due date; and the digest tells each side once per
// sitting, never about their own doing.
public sealed class HsActionThreadTests
{
    private const string ByFrance = "3490f944b29545c4b8d5a04130f42ab8";
    private const string Officer = "katy-louise.hicks@jewelbb.co.uk";
    private const string SiteManager = "james.everitt@jewelps.co.uk";

    private static SignedInUser UserWith(string email, params Role[] roles) => new(email, "Test User", roles);

    [Fact]
    public async Task AComment_movesAnOpenAction_toInProgress_andLeavesItsEvents()
    {
        await using var context = NewContext();
        var action = OpenAction(context, "10.02 Fire bell");
        await context.SaveChangesAsync();

        var comment = await new CommentOnHsRecordHandler(context).HandleAsync(
            new CommentOnHsRecord(action.HsRecordId, "Bell ordered, fitting Thursday.", SiteManager, "James Everitt"), CancellationToken.None);

        Assert.Equal("Bell ordered, fitting Thursday.", comment.Text);
        Assert.Equal((int)HsStatus.InProgress, (await context.HsRecords.SingleAsync()).Status);
        var events = await context.HsRecordEvents.OrderBy(occurrence => occurrence.Kind).ToListAsync();
        Assert.Equal(new[] { HsRecordEventKind.Commented, HsRecordEventKind.StatusChanged }, events.Select(occurrence => (HsRecordEventKind)occurrence.Kind));
        Assert.All(events, occurrence => Assert.Equal(SiteManager, occurrence.ByEmail));
        Assert.All(events, occurrence => Assert.Null(occurrence.NotifiedAt));
    }

    [Fact]
    public async Task OnlyTheOfficerOrADirector_closesACorrectiveAction()
    {
        await using var context = NewContext();
        var action = OpenAction(context, "3.01 F10");
        await context.SaveChangesAsync();
        var close = new UpdateHsRecord(action.HsRecordId, action.Summary, HsSeverity.Low, HsStatus.Closed, "", null, "JE");
        var progress = close with { Status = HsStatus.InProgress };

        Assert.False(await HsRecordCloseScope.AllowsAsync(context, UserWith(SiteManager, Role.SiteManager), close, CancellationToken.None));
        Assert.True(await HsRecordCloseScope.AllowsAsync(context, UserWith(SiteManager, Role.SiteManager), progress, CancellationToken.None));
        Assert.True(await HsRecordCloseScope.AllowsAsync(context, UserWith(Officer, Role.HealthSafetyOfficer), close, CancellationToken.None));
        Assert.True(await HsRecordCloseScope.AllowsAsync(context, UserWith("nigel@jewel.co.uk", Role.ManagingDirector), close, CancellationToken.None));
        Assert.True(HsActionRoles.Contributors.Includes(Role.SiteManager));
        Assert.False(HsActionRoles.AllowedToClose.Includes(Role.SiteManager));
    }

    [Fact]
    public async Task AStatusMoveByTheOfficer_isAnEventTheDigestReads()
    {
        await using var context = NewContext();
        var action = OpenAction(context, "8.01 Scaffolding");
        await context.SaveChangesAsync();

        await new UpdateHsRecordHandler(context).HandleAsync(
            new UpdateHsRecord(action.HsRecordId, action.Summary, HsSeverity.Low, HsStatus.Closed, "", null, "JE", Officer, "Katy-Louise Hicks"),
            CancellationToken.None);

        var occurrence = await context.HsRecordEvents.SingleAsync();
        Assert.Equal(HsRecordEventKind.StatusChanged, (HsRecordEventKind)occurrence.Kind);
        Assert.Equal("Open → Closed", occurrence.Detail);
        Assert.Equal(Officer, occurrence.ByEmail);
    }

    [Fact]
    public void AnOngoingTimeScale_hasNoDueDate()
    {
        var item = new HsAuditItemEntity { Code = "1.14", TimeScale = (int)HsAuditTimeScale.Ongoing };
        Assert.Null(HsAuditCorrectiveActions.DueDateOf(item, DateTimeOffset.UtcNow));
    }

    private static HsRecordEntity OpenAction(JpmsContext context, string summary)
    {
        var action = new HsRecordEntity
        {
            HsRecordId = Guid.NewGuid().ToString("N"), ProjectId = ByFrance, Kind = (int)HsRecordKind.CorrectiveAction,
            Summary = summary, Status = (int)HsStatus.Open, AssignedToName = "JE", RaisedAt = DateTimeOffset.UtcNow
        };
        context.HsRecords.Add(action);
        return action;
    }

    private static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"hs-thread-{Guid.NewGuid():N}").Options);
}
