using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class DeleteVariationOrderAuthorisation
{
    public bool Allows(SignedInUser user, DeleteVariationOrder command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
