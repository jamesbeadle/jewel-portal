using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class LinkVoqToRequestAuthorisation
{
    public bool Allows(SignedInUser user, LinkVoqToRequest command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
