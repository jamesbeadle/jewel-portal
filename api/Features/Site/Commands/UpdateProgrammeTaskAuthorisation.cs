using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class UpdateProgrammeTaskAuthorisation
{
    private static readonly RoleSet RolesThatMayEditProgramme = RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager);

    public bool Allows(SignedInUser user, UpdateProgrammeTask command) => RolesThatMayEditProgramme.IncludesAny(user.Roles);
}
