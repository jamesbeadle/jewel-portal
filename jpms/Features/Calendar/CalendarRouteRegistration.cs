using Jewel.JPMS.Contracts.Calendar;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Calendar;

// Client routes for project calendar events. Mirrors the api endpoints in Features/Calendar:
// list + create are project-scoped, update/delete address the event, and the Control Centre's
// "Raise calendar event" goes through the mailbox route.
public static class CalendarRouteRegistration
{
    public static IServiceCollection AddCalendarReadModels(this IServiceCollection services)
    {
        services.AddScoped<CalendarReadModel>();
        return services;
    }

    public static void RegisterCalendarRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListCalendarEventsForProject, IReadOnlyList<CalendarEvent>>(
            new QueryRoute("/api/projects/{projectId}/calendar-events",
                query => $"/api/projects/{((ListCalendarEventsForProject)query).ProjectId}/calendar-events"));

        commands.Register<CreateCalendarEvent, CalendarEvent>(
            new CommandRoute("POST", "/api/projects/{projectId}/calendar-events",
                command => $"/api/projects/{((CreateCalendarEvent)command).ProjectId}/calendar-events"));

        commands.Register<UpdateCalendarEvent, CalendarEvent>(
            new CommandRoute("PUT", "/api/calendar-events/{calendarEventId}",
                command => $"/api/calendar-events/{((UpdateCalendarEvent)command).CalendarEventId}"));

        commands.Register<DeleteCalendarEvent, Acknowledgement>(
            new CommandRoute("DELETE", "/api/calendar-events/{calendarEventId}",
                command => $"/api/calendar-events/{((DeleteCalendarEvent)command).CalendarEventId}"));

        // The Control Centre's "create new → Calendar event": raise + link the arranging email.
        commands.Register<CreateCalendarEventFromMessage, CalendarEvent>(
            new CommandRoute("POST", "/api/mailbox/message/create-calendar-event",
                _ => "/api/mailbox/message/create-calendar-event"));
    }
}
