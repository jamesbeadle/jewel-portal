using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Changes;

namespace Jewel.JPMS.Api.Features.Changes.Commands;

public sealed class UpdateChangeDetailsAuthorisation
{
    private static readonly RoleSet RolesThatMayUpdateChanges =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Architect);
    public bool Allows(SignedInUser user, UpdateChangeDetails command) => RolesThatMayUpdateChanges.Includes(user.Role);
}
