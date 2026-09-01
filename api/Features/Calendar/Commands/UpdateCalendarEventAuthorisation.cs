using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Api.Features.Calendar.Commands;

public sealed class UpdateCalendarEventAuthorisation
{
    public bool Allows(SignedInUser user, UpdateCalendarEvent command) =>
        CalendarRoles.Managers.IncludesAny(user.Roles);
}
