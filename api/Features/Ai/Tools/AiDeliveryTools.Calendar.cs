using Jewel.JPMS.Api.Features.Calendar;
using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiDeliveryTools
{
    private static AiTool ListCalendarEvents()
    {
        return new(
            "list_calendar_events",
            "A project's calendar — every dated event people need to see coming: site visits, "
            + "deliveries, meetings, subcontractor attendances. Each carries its CAL reference "
            + "(also its mailbox tag stem), kind, date (with optional start time and inclusive "
            + "end date for multi-day events), notes, and a clientVisible flag marking the "
            + "client-safe subset — events a client could be shown; the rest are internal.",
            AiToolSchema.Object(
                ("projectId", "string", "Defaults to the project in view; pass it otherwise (list_projects returns ids).", false)),
            AiToolKind.Read,
            CalendarRoles.Readers,
            ListCalendarEventsAsync);
    }

    private static async Task<string> ListCalendarEventsAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var projectId = ProjectId(context, input);
        if (string.IsNullOrWhiteSpace(projectId)) return Fail(NoProject);

        var events = await Query<ListCalendarEventsForProject, IReadOnlyList<CalendarEvent>>(
            context, new ListCalendarEventsForProject(projectId), ct);
        return Serialise(new { ok = true, projectId, count = events.Count, events = events.Select(EventRow) });
    }

    private static object EventRow(CalendarEvent item) => new
    {
        item.CalendarEventId,
        item.Reference,
        item.Title,
        kind = item.Kind.ToString(),
        date = item.Date,
        startTime = item.StartTime,
        endDate = item.EndDate,
        item.Notes,
        item.ClientVisible,
        item.CreatedByEmail
    };
}
