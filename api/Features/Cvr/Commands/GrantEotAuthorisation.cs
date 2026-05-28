using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class GrantEotAuthorisation
{
    private static readonly RoleSet RolesThatMayGrantEots = RoleSet.Of(JpmsRoles.Director);
    public bool Allows(SignedInUser user, GrantEot command) => RolesThatMayGrantEots.Includes(user.Role);
}
