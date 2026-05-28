using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Changes;

namespace Jewel.JPMS.Api.Features.Changes.Commands;

public sealed class RaiseChangeAuthorisation
{
    private static readonly RoleSet RolesThatMayRaiseChanges =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect, JpmsRoles.Subcontractor);
    public bool Allows(SignedInUser user, RaiseChange command) => RolesThatMayRaiseChanges.Includes(user.Role);
}
