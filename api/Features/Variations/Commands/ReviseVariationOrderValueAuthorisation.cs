using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class ReviseVariationOrderValueAuthorisation
{
    public bool Allows(SignedInUser user, ReviseVariationOrderValue command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
