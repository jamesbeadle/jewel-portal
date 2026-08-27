using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Calendar;

/// <summary>
/// The editable face of a calendar event — everything the Add/Edit dialog captures, shared by
/// every create/update route so the triage form and the Calendar tab cannot drift apart.
/// Date is a UK-local calendar date at midnight UTC; StartTime is "HH:mm" wall-clock text or
/// null for all-day; EndDate is the INCLUSIVE last day of a multi-day event or null.
/// </summary>
public sealed record CalendarEventDetails(
    string Title,
    CalendarEventKind Kind,
    DateTimeOffset Date,
    string? StartTime,
    DateTimeOffset? EndDate,
    string Notes,
    bool ClientVisible);

/// <summary>Adds an event from the project's Calendar tab. CreatedByEmail is stamped
/// server-side from the signed-in user.</summary>
public sealed record CreateCalendarEvent(
    string ProjectId,
    CalendarEventDetails Details,
    string CreatedByEmail = "") : ICommand<CalendarEvent>;

public sealed record UpdateCalendarEvent(
    string CalendarEventId,
    CalendarEventDetails Details) : ICommand<CalendarEvent>;

public sealed record DeleteCalendarEvent(
    string CalendarEventId) : ICommand<Acknowledgement>;
