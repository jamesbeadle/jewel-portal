using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class ApproveVariationOrderAuthorisation
{
    public bool Allows(SignedInUser user, ApproveVariationOrder command) =>
        VariationRoles.AllowedToApproveVariations.IncludesAny(user.Roles);
}
