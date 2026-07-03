using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Lads;

namespace Jewel.JPMS.Api.Features.Lads.Commands;

public sealed class AddLadClaimAuthorisation
{
    public bool Allows(SignedInUser user, AddLadClaim command) => LadRoles.AllowedToManageLads.IncludesAny(user.Roles);
}
