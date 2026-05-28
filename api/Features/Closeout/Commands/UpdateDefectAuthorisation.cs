using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class UpdateDefectAuthorisation
{
    private static readonly RoleSet RolesThatMayUpdateDefects =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager);
    public bool Allows(SignedInUser user, UpdateDefect command) => RolesThatMayUpdateDefects.IncludesAny(user.Roles);
}
