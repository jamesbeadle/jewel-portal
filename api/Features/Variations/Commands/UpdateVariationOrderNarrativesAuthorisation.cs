using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

// Editing the document's wording is record management, not a commercial act — the same manage set
// (PM and above, plus QS) that retitles a variation, per RenameVariationOrderAuthorisation.
public sealed class UpdateVariationOrderNarrativesAuthorisation
{
    public bool Allows(SignedInUser user, UpdateVariationOrderNarratives command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
