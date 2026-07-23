using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

// Un-approving is internal data repair, not a client action — the manage set (PM and above,
// plus QS), never the client, unlike approval itself.
public sealed class ReturnVariationOrderToQuotingAuthorisation
{
    public bool Allows(SignedInUser user, ReturnVariationOrderToQuoting command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
