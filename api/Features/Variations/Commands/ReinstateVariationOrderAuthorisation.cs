using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>Undoing a rejection is internal record management, not a client decision — the
/// manage set, as for returning an approved order to quoting.</summary>
public sealed class ReinstateVariationOrderAuthorisation
{
    public bool Allows(SignedInUser user, ReinstateVariationOrder command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
