using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Calendar;

/// <summary>Every event on one project's calendar, ordered by date then start time. The month
/// grid and the agenda both slice this one answer client-side — no per-month fetching.</summary>
public sealed record ListCalendarEventsForProject(
    string ProjectId) : IQuery<IReadOnlyList<CalendarEvent>>;
