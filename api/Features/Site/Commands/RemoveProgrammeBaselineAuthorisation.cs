using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class RemoveProgrammeBaselineAuthorisation
{
    private static readonly RoleSet RolesThatMayBaseline = RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, RemoveProgrammeBaseline command) => RolesThatMayBaseline.IncludesAny(user.Roles);
}
