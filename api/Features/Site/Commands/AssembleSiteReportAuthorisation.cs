using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class AssembleSiteReportAuthorisation
{
    private static readonly RoleSet RolesThatMayAssembleReports =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager);

    public bool Allows(SignedInUser user, AssembleSiteReport command) => RolesThatMayAssembleReports.IncludesAny(user.Roles);
}
