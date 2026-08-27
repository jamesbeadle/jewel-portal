namespace Jewel.JPMS.Models;

// What kind of thing is happening — drives the chip colour on the month grid and the pill on the
// agenda. Persisted as its integer value (CalendarEvents.Kind), so new members are APPENDED here
// and never inserted mid-list.
public enum CalendarEventKind
{
    SiteVisit = 0,
    Delivery = 1,
    Meeting = 2,
    SubcontractorAttendance = 3,
    Other = 4
}

public static class CalendarEventKinds
{
    public static readonly CalendarEventKind[] All =
    {
        CalendarEventKind.SiteVisit,
        CalendarEventKind.Delivery,
        CalendarEventKind.Meeting,
        CalendarEventKind.SubcontractorAttendance,
        CalendarEventKind.Other
    };

    public static string Label(CalendarEventKind kind) => kind switch
    {
        CalendarEventKind.SiteVisit => "Site visit",
        CalendarEventKind.Delivery => "Delivery",
        CalendarEventKind.Meeting => "Meeting",
        CalendarEventKind.SubcontractorAttendance => "Subcontractor attendance",
        CalendarEventKind.Other => "Other",
        _ => kind.ToString()
    };
}

// One entry on a project's calendar — a site visit, a delivery, a meeting, a subcontractor's
// attendance: anything with a date that people (and, in time, the client) need to see coming.
// Created on the project's Calendar tab or from an email at the triage stage. Each event owns a
// sequential "CAL-0001" reference which is also its mailbox tag stem, so an email tagged
// "JPMS/CAL-0001" is the event's linked mail — the same live-read link mechanism the To-do /
// Tender Enquiry families use.
//
// Date is a UK-local calendar date stored as midnight UTC (the SiteClock rule); StartTime is
// display-only "HH:mm" wall-clock text so a 09:00 visit stays 09:00 across DST changes; EndDate
// (inclusive) makes the event span several days, null = a single day.
//
// ClientVisible marks the event as safe for the client's eyes. Client logins can't reach the
// calendar yet — the flag exists from day one so the calendar is client-ready when that access
// is built, without a retrospective sort of every event ever raised.
public sealed record CalendarEvent(
    string CalendarEventId,
    string ProjectId,
    int Number,
    string Reference,        // sequential human reference, e.g. "CAL-0001" (also the tag stem)
    string Title,
    CalendarEventKind Kind,
    DateTimeOffset Date,     // UK-local calendar date at midnight UTC
    string? StartTime,       // "HH:mm" wall-clock text; null = all day
    DateTimeOffset? EndDate, // inclusive last day for a multi-day event; null = single day
    string Notes,            // free text — attendees live here for now
    bool ClientVisible,
    string CreatedByEmail,
    DateTimeOffset CreatedAt)
{
    public DateTimeOffset LastDate => EndDate ?? Date;
    public bool IsMultiDay => EndDate is { } end && end > Date;
}

/// <summary>
/// The month-grid and agenda date maths, shared by the Calendar tab and its tests (the
/// TodoItemDrafts precedent: the picture the grid promises and the buckets behind it cannot
/// drift apart). Weeks run Monday–Sunday, the UK site convention.
/// </summary>
public static class CalendarMaths
{
    /// <summary>The Monday on or before the 1st of the month — the month grid's first cell.</summary>
    public static DateOnly GridStart(int year, int month)
    {
        var first = new DateOnly(year, month, 1);
        return first.AddDays(-(((int)first.DayOfWeek + 6) % 7));
    }

    /// <summary>The grid's Monday-to-Sunday weeks, from the week of the 1st through the week of
    /// the month's last day (4–6 rows of 7).</summary>
    public static IReadOnlyList<IReadOnlyList<DateOnly>> Weeks(int year, int month)
    {
        var start = GridStart(year, month);
        var lastDay = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var weeks = new List<IReadOnlyList<DateOnly>>();
        for (var weekStart = start; weekStart <= lastDay; weekStart = weekStart.AddDays(7))
            weeks.Add(Enumerable.Range(0, 7).Select(offset => weekStart.AddDays(offset)).ToList());
        return weeks;
    }

    /// <summary>Events by calendar day: a multi-day event (inclusive EndDate) appears on EVERY
    /// day it covers. Within a day, all-day events lead and timed events follow by start time —
    /// "HH:mm" text sorts correctly on its own.</summary>
    public static Dictionary<DateOnly, List<CalendarEvent>> ByDay(IEnumerable<CalendarEvent> events)
    {
        var byDay = new Dictionary<DateOnly, List<CalendarEvent>>();
        foreach (var item in events.OrderBy(e => e.StartTime ?? "").ThenBy(e => e.Number))
        {
            var day = DateOnly.FromDateTime(item.Date.UtcDateTime);
            var last = DateOnly.FromDateTime(item.LastDate.UtcDateTime);
            for (; day <= last; day = day.AddDays(1))
            {
                if (!byDay.TryGetValue(day, out var list)) byDay[day] = list = new List<CalendarEvent>();
                list.Add(item);
            }
        }
        return byDay;
    }

    /// <summary>The agenda's rows: events still running on or after the given day (an event ends
    /// on its inclusive last day), ordered by date, then all-day first, then start time.</summary>
    public static IReadOnlyList<CalendarEvent> UpcomingFrom(IEnumerable<CalendarEvent> events, DateOnly today) =>
        events
            .Where(e => DateOnly.FromDateTime(e.LastDate.UtcDateTime) >= today)
            .OrderBy(e => e.Date)
            .ThenBy(e => e.StartTime ?? "")
            .ThenBy(e => e.Number)
            .ToList();
}
