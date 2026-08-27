using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Calendar;

/// <summary>
/// The Control Centre's "Raise calendar event": turns an email arranging something dated — a
/// site visit, a delivery slot, a meeting — into a calendar event on the project and tags the
/// email to it (JPMS/CAL-####), so the event's page reads the arranging mail live.
/// CreatedByEmail is stamped server-side from the signed-in user.
/// </summary>
public sealed record CreateCalendarEventFromMessage(
    string MessageId,
    string? InternetMessageId,
    string ProjectId,
    CalendarEventDetails Details,
    string CreatedByEmail = "",
    LinkThreadScope Scope = LinkThreadScope.ThreadBehindAnchor,
    // Explicit consent to file the thread under an additional pathway it already carries.
    // Pre-flighted before anything is created (CrossPathwayGuard), so a rejection creates nothing.
    bool AllowCrossPathway = false) : ICommand<CalendarEvent>;
