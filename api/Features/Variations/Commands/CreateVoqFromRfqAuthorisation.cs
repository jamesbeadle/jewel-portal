using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class CreateVoqFromRfqAuthorisation
{
    public bool Allows(SignedInUser user, CreateVoqFromRfq command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
