using Jewel.JPMS.Api.Features.Calendar.Commands;
using Jewel.JPMS.Api.Features.Calendar.Queries;
using Jewel.JPMS.Contracts.Calendar;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Calendar;

public static class CalendarFeatureRegistration
{
    public static IServiceCollection AddCalendarFeature(this IServiceCollection services)
    {
        // The piece the two create routes share: the numbered event row.
        services.AddScoped<CalendarEventRegister>();

        services.AddScoped<ICommandHandler<CreateCalendarEvent, CalendarEvent>, CreateCalendarEventHandler>();
        services.AddScoped<CreateCalendarEventAuthorisation>();
        services.AddScoped<CreateCalendarEventValidation>();

        services.AddScoped<ICommandHandler<UpdateCalendarEvent, CalendarEvent>, UpdateCalendarEventHandler>();
        services.AddScoped<UpdateCalendarEventAuthorisation>();
        services.AddScoped<UpdateCalendarEventValidation>();

        services.AddScoped<ICommandHandler<DeleteCalendarEvent, Acknowledgement>, DeleteCalendarEventHandler>();
        services.AddScoped<DeleteCalendarEventAuthorisation>();

        services.AddScoped<ICommandHandler<CreateCalendarEventFromMessage, CalendarEvent>, CreateCalendarEventFromMessageHandler>();
        services.AddScoped<CreateCalendarEventFromMessageAuthorisation>();
        services.AddScoped<CreateCalendarEventFromMessageValidation>();

        services.AddScoped<IQueryHandler<ListCalendarEventsForProject, IReadOnlyList<CalendarEvent>>, ListCalendarEventsForProjectHandler>();

        return services;
    }
}
