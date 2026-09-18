using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class SendVariationOrderEmailAuthorisation
{
    // Sending the variation to the client is a commercial act, so it is the circle that manages
    // variations — and NOT the client, who is on the approval gate but is the recipient here.
    public bool Allows(SignedInUser user, SendVariationOrderEmail command) =>
        VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles);
}
