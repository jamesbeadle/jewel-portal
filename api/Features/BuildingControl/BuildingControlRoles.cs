using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.BuildingControl;

/// <summary>
/// Who does what with building control. The whole internal team reads it — where the sign-offs
/// stand concerns everyone on the job. Managing (setting up the case, booking and updating
/// inspections, the files) sits with the people who actually deal with the body: the directors,
/// the PM and Site Manager who book and attend the visits, and the compliance/office roles who
/// keep the paperwork — the CalendarRoles.Managers stance. Raising an inspection from an email
/// at triage is the triage gate's business (CreateBuildingControlInspectionFromMessageAuthorisation).
///
/// External roles are deliberately absent: whether the architect/client ever sees building
/// control status is an open directors' decision (spec §8), and when that lands it gets its own
/// scoped gate — external roles are never added to internal sets by default (JpmsRoleSets).
/// </summary>
internal static class BuildingControlRoles
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
