using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

// Rejecting is the client's decision (or an internal record of it), so it carries the same set
// that may approve — PM and above, plus QS and the client.
public sealed class RejectVariationOrderAuthorisation
{
    public bool Allows(SignedInUser user, RejectVariationOrder command) =>
        VariationRoles.AllowedToApproveVariations.IncludesAny(user.Roles);
}
