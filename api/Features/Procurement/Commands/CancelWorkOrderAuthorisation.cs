using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class CancelWorkOrderAuthorisation
{
    // Deliberately narrower than raising/approving/rejecting: cancelling voids an order the
    // supplier has already been sent, so it is a directors' money decision — not something
    // the wider team that raises orders should be able to do in passing.
    private static readonly RoleSet RolesThatMayCancelOrders =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector);

    public bool Allows(SignedInUser user, CancelWorkOrder command) =>
        RolesThatMayCancelOrders.IncludesAny(user.Roles);
}
