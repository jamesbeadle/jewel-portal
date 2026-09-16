using Jewel.JPMS.Api.Features.Progress.WhatsApp;
using Xunit;

namespace Jewel.JPMS.Tests;

// The week cut Friday to Thursday and sorted by sender (2026-09-16, the FD's weekly-report spec,
// change 3): By France keeps James Everitt's messages, Les Reilly's are Ravenswood Avenue's and
// go nowhere, a stranger's go to review, and a day without messages is no day at all.
public sealed class WhatsAppWeekPlannerTests
{
    private static readonly WhatsAppWeek Week = WhatsAppWeek.EndingOn(new DateOnly(2026, 9, 10));

    private static readonly WhatsAppSenderAttribution ByFrance = new(
        thisProject: new[] { "James Everitt" },
        otherProjects: new[] { "Les Reilly" });

    private static WhatsAppMessage At(int day, int hour, string sender, string text, string? media = null) =>
        new(new DateTime(2026, 9, day, hour, 0, 0), sender, text, media);

    [Fact]
    public void Week_runsFromTheFridayToTheThursdayItIsNamedBy()
    {
        Assert.Equal(new DateOnly(2026, 9, 4), Week.Start);
        Assert.Equal(new DateOnly(2026, 9, 10), Week.End);
        Assert.Throws<ArgumentException>(() => WhatsAppWeek.EndingOn(new DateOnly(2026, 9, 9)));
    }

    [Fact]
    public void Report29sWeek_keepsByFrancesDays_andNothingFromRavenswood()
    {
        var messages = new[]
        {
            At(3, 17, "James Everitt", "Thursday before — outside the week"),
            At(4, 8, "James Everitt", "Scaffold up", "a.jpg"),
            At(4, 9, "Les Reilly", "Ravenswood soffits", "r.jpg"),
            At(7, 8, "James Everitt", "Roof stripped"),
            At(7, 12, "Unknown Person", "Which site is this?"),
            At(9, 8, "James Everitt", "", "b.jpg"),
            At(11, 8, "James Everitt", "Friday after — outside the week"),
        };

        var plan = WhatsAppWeekPlanner.Plan(messages, Week, ByFrance);

        Assert.Equal(new[] { new DateOnly(2026, 9, 4), new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 9) },
            plan.Days.Select(day => day.Date));
        Assert.Equal(new[] { "a.jpg" }, plan.Days[0].MediaFileNames);
        Assert.Equal(new[] { "b.jpg" }, plan.Days[2].MediaFileNames);
        Assert.Equal(1, plan.OtherSiteMessageCount);
        Assert.Equal(new[] { "Les Reilly" }, plan.OtherSiteSenders);
        Assert.Single(plan.Unattributed);
        Assert.Equal("Unknown Person", plan.Unattributed[0].Sender);
        Assert.Equal(2, plan.MessagesOutsideWeek);
    }

    [Fact]
    public void SenderOnThisProjectsList_winsOverAnotherProjectsList()
    {
        var both = new WhatsAppSenderAttribution(new[] { "james everitt" }, new[] { "James Everitt" });
        Assert.Equal(WhatsAppSenderSite.ThisProject, both.SiteOf(" James Everitt "));
    }

    [Fact]
    public void DayText_namesTheDay_andKeepsTheMessagesVerbatimWithTimeAndSender()
    {
        var messages = new[] { At(4, 8, "James Everitt", "Scaffold up"), At(4, 9, "James Everitt", "", "a.jpg"), At(4, 16, "James Everitt", "Off site 4") };

        Assert.Equal("Site notes — Friday 4 September 2026", WhatsAppDayText.Title(new DateOnly(2026, 9, 4)));
        Assert.Equal("08:00 James Everitt: Scaffold up\n16:00 James Everitt: Off site 4", WhatsAppDayText.Description(messages));
    }
}
