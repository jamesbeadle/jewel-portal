using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Api.Features.Calendar.Commands;

public sealed class CreateCalendarEventAuthorisation
{
    public bool Allows(SignedInUser user, CreateCalendarEvent command) =>
        CalendarRoles.Managers.IncludesAny(user.Roles);
}
