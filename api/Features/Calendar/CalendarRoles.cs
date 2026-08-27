using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Calendar;

/// <summary>
/// Who does what with the project calendar. The whole internal team reads it — what's coming up
/// on site concerns everyone. Managing events mirrors the to-do manage gate (TodoRoles), plus the
/// office roles who actually book visits and deliveries. Raising an event from an email at triage
/// is the triage gate's business, not this one's (CreateCalendarEventFromMessageAuthorisation).
///
/// Role.Client is deliberately ABSENT even though events carry ClientVisible: client logins can't
/// reach any calendar surface yet, and when that access is built it gets its own scoped gate —
/// external roles are never added to internal sets by default (JpmsRoleSets).
/// </summary>
internal static class CalendarRoles
{
    public static readonly RoleSet Readers = JpmsRoleSets.AllInternal;

    public static readonly RoleSet Managers = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.SiteManager,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin);
}
