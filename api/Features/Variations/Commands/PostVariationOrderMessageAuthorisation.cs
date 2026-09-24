using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class PostVariationOrderMessageAuthorisation
{
    // Mirrors the variation read gate — whoever can open the detail page's conversation can
    // contribute to it. The endpoint confines a client or architect to its own variations
    // (VariationOrderScope) and the handler makes whatever they post Shared (SignedInCaller).
    private static readonly RoleSet RolesThatMayPostMessages = JpmsRoleSets.DeliveryTeamAndParties;

    public bool Allows(SignedInUser user, PostVariationOrderMessage command) =>
        RolesThatMayPostMessages.IncludesAny(user.Roles);
}
