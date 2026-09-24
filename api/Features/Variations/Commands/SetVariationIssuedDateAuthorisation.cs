using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

// A correction to the record's paperwork date — the same manage set that moves a variation
// between stages and retitles it (SetVariationOrderStatusAuthorisation, RenameVariationOrderAuthorisation).
public sealed class SetVariationIssuedDateAuthorisation
{
    public bool Allows(SignedInUser user, SetVariationIssuedDate command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
