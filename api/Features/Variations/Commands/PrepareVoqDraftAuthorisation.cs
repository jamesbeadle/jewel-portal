using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class PrepareVoqDraftAuthorisation
{
    public bool Allows(SignedInUser user, PrepareVoqDraft command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
