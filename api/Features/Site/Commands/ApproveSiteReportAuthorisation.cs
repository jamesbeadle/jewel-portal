using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class ApproveSiteReportAuthorisation
{
    private static readonly RoleSet RolesThatMayApproveReports = RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, ApproveSiteReport command) => RolesThatMayApproveReports.Includes(user.Role);
}
