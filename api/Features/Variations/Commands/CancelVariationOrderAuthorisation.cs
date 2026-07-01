using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class CancelVariationOrderAuthorisation
{
    public bool Allows(SignedInUser user, CancelVariationOrder command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
