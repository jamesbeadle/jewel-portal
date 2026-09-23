using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Hs.Notifications;
using Xunit;

namespace Jewel.JPMS.Tests;

// One email per project per sitting (Katy-Louise, 15 Sep 2026): a sitting is over half an hour
// after its last change; the site manager hears what was not his doing, the officers what was not
// theirs; nobody hears about their own changes; a project with no site manager address tells him nothing.
public sealed class HsNotificationDigestTests
{
    private const string ByFrance = "by-france";
    private const string Officer = "katy-louise.hicks@jewelbb.co.uk";
    private const string SiteManager = "james.everitt@jewelps.co.uk";
    private static readonly DateTimeOffset Noon = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    private static readonly Dictionary<string, HsDigestRecipients> Recipients = new()
    {
        [ByFrance] = new HsDigestRecipients(SiteManager, new[] { Officer })
    };

    [Fact]
    public void ASittingIsOver_halfAnHourAfterItsLastChange()
    {
        var events = new[] { Event("a", Officer, Noon.AddMinutes(-40)), Event("b", Officer, Noon.AddMinutes(-10)) };

        Assert.Empty(HsNotificationDigests.Plan(events, Recipients, Noon));
        var later = HsNotificationDigests.Plan(events, Recipients, Noon.AddMinutes(25));
        var digest = Assert.Single(later);
        Assert.Equal(HsDigestAudience.SiteManager, digest.Audience);
        Assert.Equal(SiteManager, digest.To);
        Assert.Equal(2, digest.Events.Count);
    }

    [Fact]
    public void EachSide_hearsOnlyWhatWasNotItsOwnDoing()
    {
        var events = new[]
        {
            Event("a", Officer, Noon.AddHours(-2), HsRecordEventKind.StatusChanged),
            Event("b", SiteManager, Noon.AddHours(-1), HsRecordEventKind.Commented),
            Event("c", "nigel@jewel.co.uk", Noon.AddHours(-1), HsRecordEventKind.StatusChanged)
        };

        var digests = HsNotificationDigests.Plan(events, Recipients, Noon);

        var toTheSiteManager = Assert.Single(digests, digest => digest.Audience == HsDigestAudience.SiteManager);
        Assert.Equal(new[] { "a", "c" }, toTheSiteManager.Events.Select(occurrence => occurrence.HsRecordId));
        var toTheOfficer = Assert.Single(digests, digest => digest.Audience == HsDigestAudience.Officer);
        Assert.Equal(Officer, toTheOfficer.To);
        Assert.Equal(new[] { "b", "c" }, toTheOfficer.Events.Select(occurrence => occurrence.HsRecordId));
    }

    [Fact]
    public void AProjectWithNoSiteManagerAddress_tellsHimNothing_andOnlyTheOfficersDigestGoes()
    {
        var recipients = new Dictionary<string, HsDigestRecipients> { [ByFrance] = new("", new[] { Officer }) };
        var events = new[] { Event("a", SiteManager, Noon.AddHours(-1), HsRecordEventKind.Commented) };

        var digest = Assert.Single(HsNotificationDigests.Plan(events, recipients, Noon));
        Assert.Equal(HsDigestAudience.Officer, digest.Audience);
    }

    [Fact]
    public void TheDigest_readsEachEventAsASentence()
    {
        var raised = Event("a", Officer, Noon, HsRecordEventKind.Raised, "HSA-0002", "Katy-Louise Hicks");
        var comment = Event("a", SiteManager, Noon, HsRecordEventKind.Commented, "On it.", "James Everitt");
        Assert.Equal("Raised from audit HSA-0002 by Katy-Louise Hicks", HsDigestEmails.Line(raised));
        Assert.Equal("James Everitt wrote: On it.", HsDigestEmails.Line(comment));
    }

    private static HsRecordEventEntity Event(
        string hsRecordId, string byEmail, DateTimeOffset at, HsRecordEventKind kind = HsRecordEventKind.StatusChanged,
        string detail = "Open → Closed", string byName = "") => new()
    {
        HsRecordEventId = Guid.NewGuid().ToString("N"), HsRecordId = hsRecordId, ProjectId = ByFrance,
        Kind = (int)kind, Detail = detail, ByEmail = byEmail, ByName = byName, OccurredAt = at
    };
}
