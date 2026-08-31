using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class PostVariationOrderMessageAuthorisation
{
    // Internal staff plus the architect, mirroring the variation read gate — whoever can open the
    // detail page's conversation can contribute to it. Clients post through their own scoped
    // client-portal command, which forces Shared visibility; this internal path is not for them.
    private static readonly RoleSet RolesThatMayPostMessages = JpmsRoleSets.InternalAndArchitect;

    public bool Allows(SignedInUser user, PostVariationOrderMessage command) =>
        RolesThatMayPostMessages.IncludesAny(user.Roles);
}
