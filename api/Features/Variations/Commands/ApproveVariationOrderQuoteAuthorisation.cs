using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class ApproveVariationOrderQuoteAuthorisation
{
    public bool Allows(SignedInUser user, ApproveVariationOrderQuote command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
