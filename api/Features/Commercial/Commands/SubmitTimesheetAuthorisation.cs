using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SubmitTimesheetAuthorisation
{
    private static readonly RoleSet RolesThatMaySubmitTimesheets =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Subcontractor);

    public bool Allows(SignedInUser user, SubmitTimesheet command) => RolesThatMaySubmitTimesheets.Includes(user.Role);
}
