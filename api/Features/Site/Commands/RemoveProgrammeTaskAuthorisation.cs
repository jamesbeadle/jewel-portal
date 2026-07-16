using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class RemoveProgrammeTaskAuthorisation
{
    private static readonly RoleSet RolesThatMayEditProgramme = RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, RemoveProgrammeTask command) => RolesThatMayEditProgramme.IncludesAny(user.Roles);
}
