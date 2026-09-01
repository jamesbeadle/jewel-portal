using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class StageVariationOrderBuildUpAuthorisation
{
    public bool Allows(SignedInUser user, StageVariationOrderBuildUp command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
