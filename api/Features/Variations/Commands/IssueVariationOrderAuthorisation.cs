using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class IssueVariationOrderAuthorisation
{
    public bool Allows(SignedInUser user, IssueVariationOrder command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
