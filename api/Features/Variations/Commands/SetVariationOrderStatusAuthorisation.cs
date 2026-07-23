using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

// Moving a variation order between its quoting/issued stages is record management, not a client
// action — the manage set (PM and above, plus QS), same as the other housekeeping commands.
public sealed class SetVariationOrderStatusAuthorisation
{
    public bool Allows(SignedInUser user, SetVariationOrderStatus command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
