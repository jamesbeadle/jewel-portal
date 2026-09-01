using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

// Retitling is record management, not a commercial act — the same manage set (PM and above, plus
// QS) that moves a variation between stages, per SetVariationOrderStatusAuthorisation.
public sealed class RenameVariationOrderAuthorisation
{
    public bool Allows(SignedInUser user, RenameVariationOrder command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
