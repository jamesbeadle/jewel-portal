using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class ReviseVariationOrderLinesAuthorisation
{
    public bool Allows(SignedInUser user, ReviseVariationOrderLines command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
