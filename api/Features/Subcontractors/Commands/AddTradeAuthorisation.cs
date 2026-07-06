using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class AddTradeAuthorisation
{
    // Same people who curate the directory curate the trade list.
    private static readonly RoleSet RolesThatMayAddTrades =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.OfficeComplianceCoordinator);

    public bool Allows(SignedInUser user, AddTrade command) => RolesThatMayAddTrades.IncludesAny(user.Roles);
}
