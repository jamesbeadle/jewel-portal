using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

// Moving a VOQ between its tendering stages is record management, not a client action — the
// manage set (PM and above, plus QS), same as the other VOQ housekeeping commands.
public sealed class SetVoqStatusAuthorisation
{
    public bool Allows(SignedInUser user, SetVoqStatus command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
