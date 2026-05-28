using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class UpdateEotAuthorisation
{
    private static readonly RoleSet RolesThatMayUpdateEots = RoleSet.Of(JpmsRoles.Director);
    public bool Allows(SignedInUser user, UpdateEot command) => RolesThatMayUpdateEots.IncludesAny(user.Roles);
}
