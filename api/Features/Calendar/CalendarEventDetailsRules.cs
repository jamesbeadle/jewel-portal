using System.Text.RegularExpressions;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Api.Features.Calendar;

/// <summary>
/// The one place the editable face of an event is checked and applied — shared by the Calendar
/// tab's create/update and the triage create-from-message, so the two routes cannot drift apart.
/// Dates are normalised to midnight UTC on the way in (the SiteClock convention: a UK-local
/// calendar date stored as midnight UTC), so a client posting a zoned timestamp still lands on
/// the calendar day it named.
/// </summary>
internal static partial class CalendarEventDetailsRules
{
    [GeneratedRegex("^([01][0-9]|2[0-3]):[0-5][0-9]$")]
    private static partial Regex StartTimePattern();

    public static IReadOnlyList<string> Problems(CalendarEventDetails details)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(details.Title)) errors.Add("Title is required.");
        if (details.Title is { Length: > 256 }) errors.Add("Title must be 256 characters or fewer.");
        if (details.Date == default) errors.Add("Date is required.");
        if (details.StartTime is { } time && !StartTimePattern().IsMatch(time))
            errors.Add("Start time must be HH:mm (24-hour), e.g. 09:30.");
        if (details.EndDate is { } end && AsCalendarDate(end) < AsCalendarDate(details.Date))
            errors.Add("End date can't be before the start date.");
        if (details.Notes is { Length: > 4096 }) errors.Add("Notes must be 4096 characters or fewer.");
        return errors;
    }

    public static void Apply(CalendarEventEntity entity, CalendarEventDetails details)
    {
        entity.Title = details.Title.Trim();
        entity.Kind = (int)details.Kind;
        entity.Date = AsCalendarDate(details.Date);
        entity.StartTime = string.IsNullOrWhiteSpace(details.StartTime) ? null : details.StartTime;
        entity.EndDate = details.EndDate is { } end ? AsCalendarDate(end) : null;
        entity.Notes = details.Notes?.Trim() ?? "";
        entity.ClientVisible = details.ClientVisible;
    }

    /// <summary>The calendar day the caller named, as midnight UTC — date part only, whatever
    /// offset or time-of-day the serialized value arrived with.</summary>
    public static DateTimeOffset AsCalendarDate(DateTimeOffset value) =>
        new(value.Date, TimeSpan.Zero);
}
