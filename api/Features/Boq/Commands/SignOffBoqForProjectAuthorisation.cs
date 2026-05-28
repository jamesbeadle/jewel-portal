using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Boq;

namespace Jewel.JPMS.Api.Features.Boq.Commands;

public sealed class SignOffBoqForProjectAuthorisation
{
    private static readonly RoleSet RolesThatMaySignOffBoq = RoleSet.Of(JpmsRoles.Director);

    public bool Allows(SignedInUser user, SignOffBoqForProject command) =>
        RolesThatMaySignOffBoq.Includes(user.Role);
}
