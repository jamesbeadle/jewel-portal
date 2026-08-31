using System;
using System.Collections.Generic;
using System.Linq;
using Jewel.JPMS.Api.Features.Calendar;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.Calendar;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The project calendar's rules: the CAL-#### reference/tag round-trip, the shared details
// validation (title/date/end-date/time), the month-grid maths the Calendar tab renders from
// (Monday-first weeks, multi-day events on every covered day), and the pathway-neutral bucket —
// a calendar event never re-files a thread, like a to-do.
public sealed class CalendarEventTests
{
    private static CalendarEventDetails Details(
        string title = "Structural engineer site visit",
        DateTimeOffset? date = null,
        string? startTime = null,
        DateTimeOffset? endDate = null) =>
        new(title, CalendarEventKind.SiteVisit, date ?? Day(2026, 9, 3), startTime, endDate, "", false);

    private static DateTimeOffset Day(int year, int month, int day) =>
        new(new DateTime(year, month, day), TimeSpan.Zero);

    private static CalendarEvent Event(
        int number, DateTimeOffset date, DateTimeOffset? endDate = null, string? startTime = null) =>
        new($"id-{number}", "p1", number, $"CAL-{number:0000}", $"Event {number}",
            CalendarEventKind.Meeting, date, startTime, endDate, "", false, "nigel@jewel.test", DateTimeOffset.UnixEpoch);

    // ---- Reference / tag ----

    [Fact]
    public void Reference_formatsOnTheGlobalSequence()
    {
        Assert.Equal("CAL-0007", Event(7, Day(2026, 9, 1)).Reference);
        Assert.Equal("CAL-1234", Event(1234, Day(2026, 9, 1)).Reference);
    }

    [Theory]
    [InlineData("CAL-0007", 7)]
    [InlineData("CAL-1234", 1234)]
    public void TagReference_roundTripsThroughTheParser(string tag, int expected)
    {
        Assert.True(TagReferenceParsing.TryParseNumber(tag, "CAL", out var number));
        Assert.Equal(expected, number);
    }

    [Theory]
    [InlineData("TODO-0007")]
    [InlineData("CAL-")]
    [InlineData("CAL-x")]
    public void TagReference_refusesOtherFamiliesAndNoise(string tag) =>
        Assert.False(TagReferenceParsing.TryParseNumber(tag, "CAL", out _));

    // ---- Shared details validation ----

    [Fact]
    public void Problems_acceptsACompleteEvent() =>
        Assert.Empty(CalendarEventDetailsRules.Problems(Details(startTime: "09:30", endDate: Day(2026, 9, 4))));

    [Fact]
    public void Problems_requiresATitle() =>
        Assert.Contains(CalendarEventDetailsRules.Problems(Details(title: "  ")), error => error.Contains("Title"));

    [Fact]
    public void Problems_rejectsAnEndDateBeforeTheStart() =>
        Assert.Contains(
            CalendarEventDetailsRules.Problems(Details(date: Day(2026, 9, 3), endDate: Day(2026, 9, 2))),
            error => error.Contains("End date"));

    [Fact]
    public void Problems_acceptsAnEndDateEqualToTheStart() =>
        Assert.Empty(CalendarEventDetailsRules.Problems(Details(endDate: Day(2026, 9, 3))));

    [Theory]
    [InlineData("24:00")]
    [InlineData("09:60")]
    [InlineData("half nine")]
    public void Problems_rejectsMalformedStartTimes(string startTime) =>
        Assert.Contains(
            CalendarEventDetailsRules.Problems(Details(startTime: startTime)),
            error => error.Contains("Start time"));

    // "9:30" is deliberately ACCEPTED: CalendarStartTime.TryNormalise is the tolerant shared
    // reader ("8:00", "0800", "8.30am" all normalise to HH:mm) — only unreadable or impossible
    // times are refused.
    [Theory]
    [InlineData("00:00")]
    [InlineData("09:30")]
    [InlineData("9:30")]
    [InlineData("23:59")]
    public void Problems_acceptsWellFormedStartTimes(string startTime) =>
        Assert.Empty(CalendarEventDetailsRules.Problems(Details(startTime: startTime)));

    [Fact]
    public void AsCalendarDate_dropsTimeAndOffsetToMidnightUtc()
    {
        var zoned = new DateTimeOffset(2026, 9, 3, 14, 30, 0, TimeSpan.FromHours(1));
        Assert.Equal(Day(2026, 9, 3), CalendarEventDetailsRules.AsCalendarDate(zoned));
    }

    // ---- Month-grid maths ----

    [Fact]
    public void GridStart_isTheMondayOnOrBeforeTheFirst()
    {
        // June 2026 starts on a Monday — the grid starts on the 1st itself.
        Assert.Equal(new DateOnly(2026, 6, 1), CalendarMaths.GridStart(2026, 6));
        // November 2026 starts on a Sunday — the grid reaches back six days.
        Assert.Equal(new DateOnly(2026, 10, 26), CalendarMaths.GridStart(2026, 11));
    }

    [Fact]
    public void Weeks_runMondayToSundayAndCoverTheWholeMonth()
    {
        var weeks = CalendarMaths.Weeks(2026, 9); // September 2026: Tue 1st … Wed 30th
        Assert.All(weeks, week => Assert.Equal(7, week.Count));
        Assert.All(weeks, week => Assert.Equal(DayOfWeek.Monday, week[0].DayOfWeek));
        Assert.Equal(new DateOnly(2026, 8, 31), weeks[0][0]);
        Assert.Contains(new DateOnly(2026, 9, 30), weeks[^1]);
    }

    [Fact]
    public void ByDay_putsAMultiDayEventOnEveryCoveredDay()
    {
        var visit = Event(1, Day(2026, 9, 3), endDate: Day(2026, 9, 5));
        var byDay = CalendarMaths.ByDay(new[] { visit });

        Assert.Equal(3, byDay.Count);
        Assert.Contains(visit, byDay[new DateOnly(2026, 9, 3)]);
        Assert.Contains(visit, byDay[new DateOnly(2026, 9, 4)]);
        Assert.Contains(visit, byDay[new DateOnly(2026, 9, 5)]);
        Assert.False(byDay.ContainsKey(new DateOnly(2026, 9, 6)));
    }

    [Fact]
    public void ByDay_ordersAllDayFirstThenByStartTime()
    {
        var byDay = CalendarMaths.ByDay(new[]
        {
            Event(1, Day(2026, 9, 3), startTime: "14:00"),
            Event(2, Day(2026, 9, 3)),
            Event(3, Day(2026, 9, 3), startTime: "09:00"),
        });

        Assert.Equal(new[] { 2, 3, 1 }, byDay[new DateOnly(2026, 9, 3)].Select(e => e.Number));
    }

    [Fact]
    public void UpcomingFrom_keepsARunningMultiDayEventAndDropsThePast()
    {
        var past = Event(1, Day(2026, 9, 1));
        var running = Event(2, Day(2026, 9, 2), endDate: Day(2026, 9, 4));
        var future = Event(3, Day(2026, 9, 10), startTime: "08:00");

        var upcoming = CalendarMaths.UpcomingFrom(new[] { future, past, running }, new DateOnly(2026, 9, 3));

        Assert.Equal(new[] { 2, 3 }, upcoming.Select(e => e.Number));
    }

    // ---- Pathway ----

    [Fact]
    public void CalendarEvents_arePathwayNeutral() =>
        Assert.Null(TriageCategories.BucketFor(RecordType.CalendarEvent));
}
