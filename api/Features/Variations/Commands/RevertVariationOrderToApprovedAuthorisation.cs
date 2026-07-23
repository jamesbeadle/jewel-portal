using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

// Un-issuing corrects the record of instruction — the manage set (PM and above, plus QS), the
// same roles that issue and cancel.
public sealed class RevertVariationOrderToApprovedAuthorisation
{
    public bool Allows(SignedInUser user, RevertVariationOrderToApproved command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
