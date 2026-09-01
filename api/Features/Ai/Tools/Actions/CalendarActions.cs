using Jewel.JPMS.Api.Features.Calendar;
using Jewel.JPMS.Api.Features.Calendar.Commands;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>Calendar commands as connector actions. Mirrors Features/Calendar/Commands — each
/// entry's VisibleTo copies its Authorisation class's role set, and the stamps copy exactly what
/// the endpoint stamps server-side. THE EXEMPLAR FILE for the pattern: keep new area files this
/// shape.</summary>
internal sealed class CalendarActions : IAiActionSource
{
    public IEnumerable<AiAction> Build() => new[]
    {
        new AiAction(
            Name: "create_calendar_event",
            Area: "Calendar",
            Description: "Creates an event on a project's calendar — visible to the project team on the "
                + "portal's calendar immediately. Recorded as created by the signed-in user.",
            CommandType: typeof(CreateCalendarEvent),
            ResultType: typeof(CalendarEvent),
            AuthorisationType: typeof(CreateCalendarEventAuthorisation),
            ValidationType: typeof(CreateCalendarEventValidation),
            VisibleTo: CalendarRoles.Managers,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects. Dates are ISO 8601."),

        new AiAction(
            Name: "update_calendar_event",
            Area: "Calendar",
            Description: "Updates an existing calendar event's details (title, dates, notes).",
            CommandType: typeof(UpdateCalendarEvent),
            ResultType: typeof(CalendarEvent),
            AuthorisationType: typeof(UpdateCalendarEventAuthorisation),
            ValidationType: typeof(UpdateCalendarEventValidation),
            VisibleTo: CalendarRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "delete_calendar_event",
            Area: "Calendar",
            Description: "Deletes a calendar event permanently. There is no undo.",
            CommandType: typeof(DeleteCalendarEvent),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteCalendarEventAuthorisation),
            ValidationType: null,
            VisibleTo: CalendarRoles.Managers,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which event, by name and date, before calling."),

        new AiAction(
            Name: "create_calendar_event_from_message",
            Area: "Calendar",
            Description: "Creates a calendar event from a mailbox message (triage pathway) — reads the "
                + "email and books the event it describes.",
            CommandType: typeof(CreateCalendarEventFromMessage),
            ResultType: typeof(CalendarEvent),
            AuthorisationType: typeof(CreateCalendarEventFromMessageAuthorisation),
            ValidationType: typeof(CreateCalendarEventFromMessageValidation),
            VisibleTo: TriageRoles.AllowedToTriage,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "messageId is a mailbox message id from the triage queue, not a request id.")
    };
}
