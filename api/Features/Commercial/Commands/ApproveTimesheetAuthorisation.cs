using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class ApproveTimesheetAuthorisation
{
    private static readonly RoleSet RolesThatMayApproveTimesheets =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, ApproveTimesheet command) => RolesThatMayApproveTimesheets.IncludesAny(user.Roles);
}
