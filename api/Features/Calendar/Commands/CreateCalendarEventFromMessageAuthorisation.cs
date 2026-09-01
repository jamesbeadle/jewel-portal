using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Api.Features.Calendar.Commands;

/// <summary>Raising an event from an email is a triage act, so it carries the triage gate —
/// the same stance as CreateTodoItemsFromMessage.</summary>
public sealed class CreateCalendarEventFromMessageAuthorisation
{
    public bool Allows(SignedInUser user, CreateCalendarEventFromMessage command) =>
        TriageRoles.AllowedToTriage.IncludesAny(user.Roles);
}
