using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

// The estimate is quoting-stage register data with no commercial writes behind it — the same
// manage set (PM and above, plus QS) that moves a variation between stages and retitles it.
public sealed class SetVariationOrderEstimateAuthorisation
{
    public bool Allows(SignedInUser user, SetVariationOrderEstimate command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
