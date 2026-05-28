using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class RaiseDefectAuthorisation
{
    private static readonly RoleSet RolesThatMayRaiseDefects =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Client, JpmsRoles.Architect);
    public bool Allows(SignedInUser user, RaiseDefect command) => RolesThatMayRaiseDefects.IncludesAny(user.Roles);
}
