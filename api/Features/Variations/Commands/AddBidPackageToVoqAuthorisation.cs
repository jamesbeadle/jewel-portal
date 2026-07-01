using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class AddBidPackageToVoqAuthorisation
{
    public bool Allows(SignedInUser user, AddBidPackageToVoq command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
