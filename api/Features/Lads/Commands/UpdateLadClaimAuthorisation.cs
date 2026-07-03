using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Lads;

namespace Jewel.JPMS.Api.Features.Lads.Commands;

public sealed class UpdateLadClaimAuthorisation
{
    public bool Allows(SignedInUser user, UpdateLadClaim command) => LadRoles.AllowedToManageLads.IncludesAny(user.Roles);
}
