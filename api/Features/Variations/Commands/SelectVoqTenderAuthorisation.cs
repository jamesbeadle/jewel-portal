using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class SelectVoqTenderAuthorisation
{
    public bool Allows(SignedInUser user, SelectVoqTender command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
