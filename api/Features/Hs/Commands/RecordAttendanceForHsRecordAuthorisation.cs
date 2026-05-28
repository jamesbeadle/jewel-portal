using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Commands;

public sealed class RecordAttendanceForHsRecordAuthorisation
{
    private static readonly RoleSet RolesThatMayRecordAttendance =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead);

    public bool Allows(SignedInUser user, RecordAttendanceForHsRecord command) => RolesThatMayRecordAttendance.Includes(user.Role);
}
