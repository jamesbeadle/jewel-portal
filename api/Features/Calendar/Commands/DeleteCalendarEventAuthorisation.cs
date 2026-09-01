using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Api.Features.Calendar.Commands;

public sealed class DeleteCalendarEventAuthorisation
{
    public bool Allows(SignedInUser user, DeleteCalendarEvent command) =>
        CalendarRoles.Managers.IncludesAny(user.Roles);
}
